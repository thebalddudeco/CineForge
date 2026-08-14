from __future__ import annotations

"""Native Wan 2.2 I2V loader for the CineForge scaled-FP8 model pack.

The checkpoint name conversion follows the Apache-2.0 Diffusers Wan conversion
script.  Inference itself is implemented here and has no ComfyUI dependency.
"""

import json
import types
from pathlib import Path
from typing import Any, Callable


PACK_ID = "cineforge-wan22-i2v-a14b-fp8"
PACK_FILES = {
    "high": "wan2.2_i2v_high_noise_14B_fp8_scaled.safetensors",
    "low": "wan2.2_i2v_low_noise_14B_fp8_scaled.safetensors",
    "text": "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
    "vae": "wan_2.1_vae.safetensors",
}

TRANSFORMER_RENAMES = {
    "time_embedding.0": "condition_embedder.time_embedder.linear_1",
    "time_embedding.2": "condition_embedder.time_embedder.linear_2",
    "text_embedding.0": "condition_embedder.text_embedder.linear_1",
    "text_embedding.2": "condition_embedder.text_embedder.linear_2",
    "time_projection.1": "condition_embedder.time_proj",
    "head.modulation": "scale_shift_table",
    "head.head": "proj_out",
    "modulation": "scale_shift_table",
    "ffn.0": "ffn.net.0.proj",
    "ffn.2": "ffn.net.2",
    "norm2": "norm__placeholder",
    "norm3": "norm2",
    "norm__placeholder": "norm3",
    "self_attn.q": "attn1.to_q",
    "self_attn.k": "attn1.to_k",
    "self_attn.v": "attn1.to_v",
    "self_attn.o": "attn1.to_out.0",
    "self_attn.norm_q": "attn1.norm_q",
    "self_attn.norm_k": "attn1.norm_k",
    "cross_attn.q": "attn2.to_q",
    "cross_attn.k": "attn2.to_k",
    "cross_attn.v": "attn2.to_v",
    "cross_attn.o": "attn2.to_out.0",
    "cross_attn.norm_q": "attn2.norm_q",
    "cross_attn.norm_k": "attn2.norm_k",
}


def find_pack(root: Path) -> Path | None:
    """Find either an installed components/ pack or a flat developer pack."""
    candidates = [root]
    if root.is_dir():
        candidates.extend(path.parent for path in root.rglob("cineforge-model.json"))
    for candidate in candidates:
        manifest = candidate / "cineforge-model.json"
        components = candidate / "components"
        base = components if components.is_dir() else candidate
        if manifest.is_file() and all((base / name).is_file() for name in PACK_FILES.values()):
            try:
                if json.loads(manifest.read_text(encoding="utf-8")).get("id") == PACK_ID:
                    return candidate
            except (OSError, json.JSONDecodeError):
                pass
    return None


def _component(pack: Path, name: str) -> Path:
    nested = pack / "components" / name
    return nested if nested.is_file() else pack / name


def _rename_transformer_key(key: str) -> str:
    for old, new in TRANSFORMER_RENAMES.items():
        key = key.replace(old, new)
    return key


def _read_scaled_state(path: Path, rename: Callable[[str], str] | None = None):
    from safetensors import safe_open

    state: dict[str, Any] = {}
    scales: dict[str, tuple[Any, Any]] = {}
    rename = rename or (lambda value: value)
    with safe_open(str(path), framework="pt", device="cpu") as archive:
        names = set(archive.keys())
        for source in names:
            if source in {"scaled_fp8", "spiece_model"} or source.endswith((".scale_weight", ".scale_input")):
                continue
            target = rename(source)
            state[target] = archive.get_tensor(source)
            if source.endswith(".weight"):
                stem = source[:-7]
                weight_scale = stem + ".scale_weight"
                if weight_scale in names:
                    input_scale = stem + ".scale_input"
                    scales[target[:-7]] = (
                        archive.get_tensor(weight_scale),
                        archive.get_tensor(input_scale) if input_scale in names else None,
                    )
    return state, scales


def _module(model: Any, dotted: str) -> Any:
    current = model
    for part in dotted.split("."):
        current = current[int(part)] if part.isdigit() else getattr(current, part)
    return current


def _fp8_linear_forward(self: Any, input_tensor: Any) -> Any:
    import torch
    import torch.nn.functional as functional

    compute_dtype = torch.float16 if input_tensor.dtype == torch.float8_e4m3fn else input_tensor.dtype
    if input_tensor.device.type != "cuda":
        weight = self.weight.to(dtype=compute_dtype) * self.cineforge_scale_weight.to(compute_dtype)
        bias = self.bias.to(compute_dtype) if self.bias is not None else None
        return functional.linear(input_tensor.to(compute_dtype), weight, bias)
    original_shape = input_tensor.shape
    flat = input_tensor.reshape(-1, original_shape[-1]).contiguous()
    fp8_input = (
        flat.contiguous()
        if flat.dtype == torch.float8_e4m3fn
        else flat.clamp(-448, 448).to(torch.float8_e4m3fn).contiguous()
    )
    scale_a = self.cineforge_scale_input.to(device=input_tensor.device, dtype=torch.float32)
    scale_b = self.cineforge_scale_weight.to(device=input_tensor.device, dtype=torch.float32)
    output = torch._scaled_mm(
        fp8_input,
        self.weight.t(),
        scale_a=scale_a,
        scale_b=scale_b,
        out_dtype=compute_dtype,
    )
    if self.bias is not None:
        output.add_(self.bias.to(dtype=compute_dtype))
    return output.reshape(*original_shape[:-1], self.out_features)


def _install_scaled_linears(model: Any, scales: dict[str, tuple[Any, Any]]) -> None:
    import torch

    for name, (weight_scale, input_scale) in scales.items():
        layer = _module(model, name)
        if not isinstance(layer, torch.nn.Linear):
            raise TypeError(f"Scaled FP8 tensor {name} does not resolve to a Linear layer")
        layer.register_buffer("cineforge_scale_weight", weight_scale.float().reshape(()), persistent=False)
        layer.register_buffer(
            "cineforge_scale_input",
            (input_scale.float().reshape(()) if input_scale is not None else torch.ones((), dtype=torch.float32)),
            persistent=False,
        )
        layer.forward = types.MethodType(_fp8_linear_forward, layer)


def _umt5_dense_gated_forward(self: Any, hidden_states: Any) -> Any:
    hidden_gelu = self.act(self.wi_0(hidden_states))
    hidden_linear = self.wi_1(hidden_states)
    return self.wo(self.dropout(hidden_gelu * hidden_linear))


def _umt5_dense_forward(self: Any, hidden_states: Any) -> Any:
    return self.wo(self.dropout(self.act(self.wi(hidden_states))))


def _install_umt5_safe_ffn(model: Any) -> None:
    """Bypass Transformers' direct activation-to-weight-dtype FP8 cast.

    The stock UMT5 wrapper casts the feed-forward activation to the ``wo``
    weight dtype. For scaled-FP8 checkpoints that skips the required clamp and
    can create NaNs for prompt-dependent activation ranges.
    """
    for module in model.modules():
        if all(hasattr(module, name) for name in ("wi_0", "wi_1", "wo", "act", "dropout")):
            module.forward = types.MethodType(_umt5_dense_gated_forward, module)
        elif all(hasattr(module, name) for name in ("wi", "wo", "act", "dropout")):
            module.forward = types.MethodType(_umt5_dense_forward, module)


def _load_transformer(path: Path, config_path: Path, progress: Callable[[str], None]):
    import torch
    from diffusers import WanTransformer3DModel
    from diffusers.models.transformers.transformer_wan import WanRotaryPosEmbed

    config = json.loads(config_path.read_text(encoding="utf-8"))
    with torch.device("meta"):
        model = WanTransformer3DModel.from_config(config)
    state, scales = _read_scaled_state(path, _rename_transformer_key)
    state.pop("scaled_fp8", None)
    result = model.load_state_dict(state, strict=True, assign=True)
    if result.missing_keys or result.unexpected_keys:
        raise RuntimeError(f"Wan transformer mismatch: {result}")
    # Rotary tables are non-persistent buffers, so they are not present in the
    # checkpoint and must be materialized after meta-device construction.
    rope = WanRotaryPosEmbed(
        int(config.get("attention_head_dim", 128)),
        tuple(config.get("patch_size", (1, 2, 2))),
        int(config.get("rope_max_seq_len", 1024)),
    )
    model.rope.freqs_cos = rope.freqs_cos
    model.rope.freqs_sin = rope.freqs_sin
    _install_scaled_linears(model, scales)
    model.requires_grad_(False).eval()
    progress(f"Loaded {path.name}")
    return model


def _load_text_encoder(path: Path, config_path: Path, progress: Callable[[str], None]):
    import torch
    from transformers import UMT5Config, UMT5EncoderModel

    config = UMT5Config.from_dict(json.loads(config_path.read_text(encoding="utf-8")))
    with torch.device("meta"):
        model = UMT5EncoderModel(config)
    state, scales = _read_scaled_state(path)
    state["encoder.embed_tokens.weight"] = state["shared.weight"]
    result = model.load_state_dict(state, strict=True, assign=True)
    if result.missing_keys or result.unexpected_keys:
        raise RuntimeError(f"Wan text encoder mismatch: {result}")
    _install_scaled_linears(model, scales)
    _install_umt5_safe_ffn(model)
    # Assign the same Parameter object, not merely the same storage, so moving
    # the encoder does not duplicate the 2+ GB shared embedding table.
    model.encoder.embed_tokens.weight = model.shared.weight
    model.requires_grad_(False).eval()
    progress(f"Loaded {path.name}")
    return model


def _convert_vae_state(old: dict[str, Any]) -> dict[str, Any]:
    middle = {
        "encoder.middle.0.residual.0.gamma": "encoder.mid_block.resnets.0.norm1.gamma",
        "encoder.middle.0.residual.2.bias": "encoder.mid_block.resnets.0.conv1.bias",
        "encoder.middle.0.residual.2.weight": "encoder.mid_block.resnets.0.conv1.weight",
        "encoder.middle.0.residual.3.gamma": "encoder.mid_block.resnets.0.norm2.gamma",
        "encoder.middle.0.residual.6.bias": "encoder.mid_block.resnets.0.conv2.bias",
        "encoder.middle.0.residual.6.weight": "encoder.mid_block.resnets.0.conv2.weight",
        "encoder.middle.2.residual.0.gamma": "encoder.mid_block.resnets.1.norm1.gamma",
        "encoder.middle.2.residual.2.bias": "encoder.mid_block.resnets.1.conv1.bias",
        "encoder.middle.2.residual.2.weight": "encoder.mid_block.resnets.1.conv1.weight",
        "encoder.middle.2.residual.3.gamma": "encoder.mid_block.resnets.1.norm2.gamma",
        "encoder.middle.2.residual.6.bias": "encoder.mid_block.resnets.1.conv2.bias",
        "encoder.middle.2.residual.6.weight": "encoder.mid_block.resnets.1.conv2.weight",
        "decoder.middle.0.residual.0.gamma": "decoder.mid_block.resnets.0.norm1.gamma",
        "decoder.middle.0.residual.2.bias": "decoder.mid_block.resnets.0.conv1.bias",
        "decoder.middle.0.residual.2.weight": "decoder.mid_block.resnets.0.conv1.weight",
        "decoder.middle.0.residual.3.gamma": "decoder.mid_block.resnets.0.norm2.gamma",
        "decoder.middle.0.residual.6.bias": "decoder.mid_block.resnets.0.conv2.bias",
        "decoder.middle.0.residual.6.weight": "decoder.mid_block.resnets.0.conv2.weight",
        "decoder.middle.2.residual.0.gamma": "decoder.mid_block.resnets.1.norm1.gamma",
        "decoder.middle.2.residual.2.bias": "decoder.mid_block.resnets.1.conv1.bias",
        "decoder.middle.2.residual.2.weight": "decoder.mid_block.resnets.1.conv1.weight",
        "decoder.middle.2.residual.3.gamma": "decoder.mid_block.resnets.1.norm2.gamma",
        "decoder.middle.2.residual.6.bias": "decoder.mid_block.resnets.1.conv2.bias",
        "decoder.middle.2.residual.6.weight": "decoder.mid_block.resnets.1.conv2.weight",
    }
    direct = {
        **middle,
        "encoder.middle.1.norm.gamma": "encoder.mid_block.attentions.0.norm.gamma",
        "encoder.middle.1.to_qkv.weight": "encoder.mid_block.attentions.0.to_qkv.weight",
        "encoder.middle.1.to_qkv.bias": "encoder.mid_block.attentions.0.to_qkv.bias",
        "encoder.middle.1.proj.weight": "encoder.mid_block.attentions.0.proj.weight",
        "encoder.middle.1.proj.bias": "encoder.mid_block.attentions.0.proj.bias",
        "decoder.middle.1.norm.gamma": "decoder.mid_block.attentions.0.norm.gamma",
        "decoder.middle.1.to_qkv.weight": "decoder.mid_block.attentions.0.to_qkv.weight",
        "decoder.middle.1.to_qkv.bias": "decoder.mid_block.attentions.0.to_qkv.bias",
        "decoder.middle.1.proj.weight": "decoder.mid_block.attentions.0.proj.weight",
        "decoder.middle.1.proj.bias": "decoder.mid_block.attentions.0.proj.bias",
        "encoder.head.0.gamma": "encoder.norm_out.gamma",
        "encoder.head.2.bias": "encoder.conv_out.bias",
        "encoder.head.2.weight": "encoder.conv_out.weight",
        "decoder.head.0.gamma": "decoder.norm_out.gamma",
        "decoder.head.2.bias": "decoder.conv_out.bias",
        "decoder.head.2.weight": "decoder.conv_out.weight",
        "conv1.weight": "quant_conv.weight",
        "conv1.bias": "quant_conv.bias",
        "conv2.weight": "post_quant_conv.weight",
        "conv2.bias": "post_quant_conv.bias",
        "encoder.conv1.weight": "encoder.conv_in.weight",
        "encoder.conv1.bias": "encoder.conv_in.bias",
        "decoder.conv1.weight": "decoder.conv_in.weight",
        "decoder.conv1.bias": "decoder.conv_in.bias",
    }
    converted: dict[str, Any] = {}
    residual_names = {
        ".residual.0.gamma": ".norm1.gamma", ".residual.2.bias": ".conv1.bias",
        ".residual.2.weight": ".conv1.weight", ".residual.3.gamma": ".norm2.gamma",
        ".residual.6.bias": ".conv2.bias", ".residual.6.weight": ".conv2.weight",
        ".shortcut.bias": ".conv_shortcut.bias", ".shortcut.weight": ".conv_shortcut.weight",
    }
    for key, value in old.items():
        if key in direct:
            converted[direct[key]] = value
        elif key.startswith("encoder.downsamples."):
            target = key.replace("encoder.downsamples.", "encoder.down_blocks.")
            for source, replacement in residual_names.items():
                target = target.replace(source, replacement)
            converted[target] = value
        elif key.startswith("decoder.upsamples."):
            block = int(key.split(".")[2])
            if ".residual." in key and block in {0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14}:
                group = block // 4
                resnet = block % 4
                target = key
                for source, replacement in residual_names.items():
                    target = target.replace(source, replacement)
                suffix = target.split(f"decoder.upsamples.{block}.", 1)[1]
                converted[f"decoder.up_blocks.{group}.resnets.{resnet}.{suffix}"] = value
            elif ".shortcut." in key:
                if block == 4:
                    target = key.replace("decoder.upsamples.4", "decoder.up_blocks.1").replace(".shortcut.", ".resnets.0.conv_shortcut.")
                else:
                    target = key.replace("decoder.upsamples.", "decoder.up_blocks.").replace(".shortcut.", ".conv_shortcut.")
                converted[target] = value
            elif ".resample." in key or ".time_conv." in key:
                groups = {3: 0, 7: 1, 11: 2}
                target = key.replace(f"decoder.upsamples.{block}", f"decoder.up_blocks.{groups[block]}.upsamplers.0") if block in groups else key.replace("decoder.upsamples.", "decoder.up_blocks.")
                converted[target] = value
            else:
                converted[key.replace("decoder.upsamples.", "decoder.up_blocks.")] = value
        else:
            converted[key] = value
    return converted


def _load_vae(path: Path, config_path: Path, progress: Callable[[str], None]):
    import torch
    from diffusers import AutoencoderKLWan
    from safetensors.torch import load_file

    config = json.loads(config_path.read_text(encoding="utf-8"))
    with torch.device("meta"):
        model = AutoencoderKLWan.from_config(config)
    state = _convert_vae_state(load_file(str(path), device="cpu"))
    result = model.load_state_dict(state, strict=True, assign=True)
    if result.missing_keys or result.unexpected_keys:
        raise RuntimeError(f"Wan VAE mismatch: {result}")
    # Wan's VAE is numerically unstable in reduced precision during decode.
    # Diffusers' Wan guidance likewise keeps this component in float32.
    model.to(dtype=torch.float32)
    model.requires_grad_(False).eval()
    progress(f"Loaded {path.name}")
    return model


def load_pipeline(pack: Path, progress: Callable[[str], None] | None = None):
    import torch
    from diffusers import UniPCMultistepScheduler, WanImageToVideoPipeline
    from transformers import AutoTokenizer

    progress = progress or (lambda _message: None)
    if not torch.cuda.is_available():
        raise RuntimeError("CineForge Desktop requires a supported NVIDIA CUDA GPU for Wan 2.2 generation.")
    support = pack / "support"
    required = [support / "transformer/config.json", support / "transformer_2/config.json", support / "text_encoder/config.json", support / "vae/config.json", support / "tokenizer/tokenizer_config.json"]
    missing = [str(path.relative_to(pack)) for path in required if not path.is_file()]
    if missing:
        raise RuntimeError("The CineForge Wan pack is incomplete; missing " + ", ".join(missing))

    high = _load_transformer(_component(pack, PACK_FILES["high"]), support / "transformer/config.json", progress)
    low = _load_transformer(_component(pack, PACK_FILES["low"]), support / "transformer_2/config.json", progress)
    text_encoder = _load_text_encoder(_component(pack, PACK_FILES["text"]), support / "text_encoder/config.json", progress)
    vae = _load_vae(_component(pack, PACK_FILES["vae"]), support / "vae/config.json", progress)
    tokenizer = AutoTokenizer.from_pretrained(support / "tokenizer", local_files_only=True, use_fast=True)
    scheduler = UniPCMultistepScheduler.from_pretrained(support / "scheduler", local_files_only=True)
    pipeline = WanImageToVideoPipeline(
        transformer=high, transformer_2=low, text_encoder=text_encoder, tokenizer=tokenizer,
        vae=vae, scheduler=scheduler, boundary_ratio=0.9,
    )
    pipeline.set_progress_bar_config(disable=True)
    device = torch.device("cuda")
    for model in (high, low):
        model.enable_group_offload(onload_device=device, offload_device=torch.device("cpu"), offload_type="block_level", num_blocks_per_group=1)
    vae.enable_tiling()
    vae.enable_slicing()
    vae.enable_group_offload(onload_device=device, offload_device=torch.device("cpu"), offload_type="block_level", num_blocks_per_group=1)
    progress("Native Wan 2.2 pipeline ready")
    return pipeline


def encode_prompt(pipeline: Any, prompt: str, negative_prompt: str | None):
    import torch

    device = torch.device("cuda")
    encoder = pipeline.text_encoder
    encoder.to(device)
    try:
        with torch.inference_mode():
            embeds = pipeline.encode_prompt(
                prompt=prompt,
                negative_prompt=negative_prompt,
                do_classifier_free_guidance=True,
                device=device,
                dtype=torch.float16,
                max_sequence_length=226,
            )
    finally:
        encoder.to("cpu")
        torch.cuda.empty_cache()
    return embeds
