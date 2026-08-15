# Release process

This document is the required paper trail for every CineForge release.

## Release gates

1. **Scope:** confirm the release remains Wan-only and does not introduce a hidden external runtime dependency.
2. **Source:** all intended changes are reviewed, tested, committed, and represented in `CHANGELOG.md`.
3. **Models:** update `MODEL_COMPATIBILITY.md` with exact repository revision, format, checksums, and test results.
4. **Verification:** run unit tests, application startup, model discovery, a real generation, output playback, clean shutdown, a clean-machine installer check, and an in-place upgrade from the previous public release using the same application and library paths.
5. **Privacy:** verify local inputs and outputs remain local except for explicit downloads or user-initiated publishing.
6. **Security:** scan dependencies and ensure no token, local path, private media, or model weight is committed.
7. **Packaging:** build the Windows x64 installer, sign it when a certificate is available, and calculate SHA-256.
8. **Documentation:** write release notes covering additions, changes, fixes, known issues, requirements, and upgrade behavior.

## Versioning

- Patch: compatible fixes and documentation corrections.
- Minor: backward-compatible features, model-pack additions, or substantial UI changes.
- Major: incompatible project/model schema, runtime, or distribution changes.

Pre-1.0 releases may change rapidly, but all changes must still be documented.

## GitHub release checklist

- [ ] Create a release branch or release pull request.
- [ ] Move completed `Unreleased` entries into a dated version section.
- [ ] Update application and installer version constants.
- [ ] Update compatibility and verification records.
- [ ] Run all release gates.
- [ ] Merge to the default branch.
- [ ] Create an annotated `vX.Y.Z` tag.
- [ ] Let the Windows release workflow build the artifact.
- [ ] Attach installer, checksums, manifest, release notes, and verification summary.
- [ ] Verify downloads from the public release page.
- [ ] Link the exact Hugging Face model revision.
- [ ] Install over the previous public version and verify that the application folder contains only the new release while the existing CineForge Library, models, projects, inputs, outputs, cache, preferences, and partial downloads remain intact.

## Patch records

Every fix must have at least one traceable GitHub issue or pull request, a changelog entry when user-visible, a test or documented reproduction, and the release version containing the fix. Emergency patches follow the same requirements and must not be edited silently after release.
