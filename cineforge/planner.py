from __future__ import annotations

import hashlib
import json
import re
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
from typing import Any


DELIMITER = "----"


@dataclass
class Shot:
    id: str
    branch: str
    index: int
    title: str
    story_change: str
    prompt: str
    motion_prompt: str
    continuity_checks: list[str]
    approved: bool = False
    still_job_id: str | None = None
    still_output: dict[str, Any] | None = None
    video_job_id: str | None = None
    video_output: dict[str, Any] | None = None


ANGLE_SPECS = [
    ("Environmental master", "32mm", "wide three-quarter view", "locked-off with a restrained push-in"),
    ("Lateral pressure", "50mm", "medium lateral profile", "measured dolly tracking"),
    ("Subjective portrait", "85mm", "tight three-quarter portrait", "near-static handheld drift"),
    ("Surveillance compression", "135mm", "obstructed long-lens view", "locked telephoto observation"),
    ("Disruptive geography", "24mm", "low or overhead geography-reset angle", "slow tilt revealing spatial stakes"),
]

INSERT_SPECS = [
    ("Time pressure", "100mm macro", "a timekeeping, countdown, or deadline detail", "micro slider move"),
    ("Physical cost", "85mm close focus", "hands, breath, sweat, fabric, or strain", "restrained handheld observation"),
    ("Environmental friction", "50mm close focus", "surface, weather, machinery, traffic, or debris affecting action", "short lateral track"),
    ("Story evidence", "75mm", "one canon-supported object carrying information", "controlled rack focus"),
    ("Behavioral tell", "100mm", "eyes, jaw, knuckles, posture, or a tiny involuntary reaction", "locked frame with natural body motion"),
]

PROGRESSION_SPECS = [
    ("Objective", "35mm", "establish the immediate objective and usable geography", "The goal becomes visually legible."),
    ("Threat", "65mm", "introduce pursuit, surveillance, obstruction, or threat", "A force now opposes the objective."),
    ("Decision", "50mm", "force the subject to react or make a physical decision", "The subject commits to a course of action."),
    ("Escalation", "85mm", "close distance or reveal consequential new information", "The cost or danger increases."),
    ("Consequence", "40mm", "land on a consequence, reveal, or unresolved danger", "The ending motivates the next shot."),
]


def _clean(value: Any, fallback: str) -> str:
    text = re.sub(r"\s+", " ", str(value or "")).strip()
    return text or fallback


def _project_id(payload: dict[str, Any]) -> str:
    material = json.dumps(payload, sort_keys=True).encode("utf-8") + datetime.now(timezone.utc).isoformat().encode()
    return hashlib.sha1(material).hexdigest()[:12]


def _continuity(payload: dict[str, Any]) -> str:
    locks = _clean(payload.get("continuity"), "identity, wardrobe, props, geography, weather, time of day, and screen direction")
    return f"Continuity lock: preserve {locks}. Do not introduce unsupported people, props, locations, text, or events."


def _look(payload: dict[str, Any]) -> str:
    return _clean(payload.get("look"), "cinematic naturalism, motivated practical light, textured shadows, restrained halation, subtle grain, realistic skin and materials")


def _motion_prompt(payload: dict[str, Any], title: str, action: str, camera: str, lens: str, end_beat: str) -> str:
    duration = max(2, min(15, int(payload.get("duration") or 5)))
    subject = _clean(payload.get("subject"), "the principal subject")
    environment = _clean(payload.get("environment"), "the established environment")
    return (
        f"Create one {duration}-second cinematic shot from the approved reference frame. "
        f"SUBJECT LOCK: Preserve {subject} exactly, including identity, wardrobe, physical state, and carried props. "
        f"ACTION: {action}. CAMERA: {camera}. LENS/FRAMING: {lens}, composition from the approved still. "
        f"ENVIRONMENTAL MOTION: one restrained, physically motivated layer from {environment}. "
        f"START FRAME: match the approved frame exactly. END BEAT: {end_beat}. "
        f"CONTINUITY: maintain screen direction, geography, eyeline, weather, light direction, background density, and prop state. "
        f"LOOK: {_look(payload)}. No identity drift, morphing, sliding feet, floating objects, speed ramps, impossible stabilization, or unmotivated camera motion."
    )


def _shot(payload: dict[str, Any], branch: str, index: int, title: str, lens: str, framing: str, movement: str, task: str, change: str) -> Shot:
    subject = _clean(payload.get("subject"), "the principal subject")
    action = _clean(payload.get("action"), "pursues the immediate objective under pressure")
    environment = _clean(payload.get("environment"), "the established story environment")
    objective = _clean(payload.get("objective"), "complete the immediate objective")
    obstacle = _clean(payload.get("obstacle"), "an emerging obstacle")
    lighting = _clean(payload.get("lighting"), "motivated practical light shaped with negative fill")
    end_beat = change
    prompt = (
        f"{title}. {subject} {action} in {environment}. Shot task: {task}; the objective is to {objective}, while {obstacle}. "
        f"Camera: {framing}, {lens}, {movement}. Lighting: {lighting}. "
        f"Practical atmosphere and environmental behavior remain subtle and physically plausible. {_continuity(payload)} "
        f"Visual language: {_look(payload)}. End beat: {end_beat}"
    )
    checks = [
        "Identity and wardrobe match the reference pack",
        "Props, geography, weather, and light direction remain stable",
        "Screen direction and eyeline cut with adjacent shots",
        "No unsupported text, objects, or story events appear",
        "Hands, faces, architecture, and background figures remain anatomically stable",
    ]
    return Shot(
        id=f"{branch}-{index + 1}", branch=branch, index=index, title=title,
        story_change=change, prompt=prompt,
        motion_prompt=_motion_prompt(payload, title, action, movement, lens, end_beat),
        continuity_checks=checks,
    )


def build_plan(payload: dict[str, Any]) -> dict[str, Any]:
    branches: dict[str, list[dict[str, Any]]] = {"angles": [], "inserts": [], "progression": []}
    for index, (title, lens, framing, movement) in enumerate(ANGLE_SPECS):
        item = _shot(payload, "angles", index, title, lens, framing, movement, "reveal a distinct camera angle without changing the story beat", "Camera position changes while the story state remains locked.")
        branches["angles"].append(asdict(item))
    for index, (title, lens, task, movement) in enumerate(INSERT_SPECS):
        item = _shot(payload, "inserts", index, title, lens, "editorial insert", movement, task, "A tactile detail reveals tension, information, time pressure, or physical cost.")
        branches["inserts"].append(asdict(item))
    for index, (title, lens, task, change) in enumerate(PROGRESSION_SPECS):
        item = _shot(payload, "progression", index, title, lens, "story-progressing coverage", "motivated dolly, track, pan, tilt, handheld, or locked observation", task, change)
        branches["progression"].append(asdict(item))
    assert all(len(items) == 5 for items in branches.values())
    project_id = _project_id(payload)
    return {
        "schema_version": 1,
        "project_id": project_id,
        "title": _clean(payload.get("title"), "Untitled cinematic project"),
        "created_at": datetime.now(timezone.utc).isoformat(),
        "brief": payload,
        "delimiter": DELIMITER,
        "branches": branches,
        "selection": [],
        "status": "planned",
    }
