# ServerSync

Both `dependencies/ServerSync.dll` and
`src/ValheimRAFT.Unity/Assets/Plugins/ServerSync/ServerSync.dll` are the unmodified
official **ServerSync v1.20** release asset. Keep these copies identical.

- Upstream: https://github.com/blaxxun-boop/ServerSync
- Release: https://github.com/blaxxun-boop/ServerSync/releases/tag/v1.20
- Published: 2026-09-09; release note: "Recompile for 1.0"
- Source commit: `c57c2aa54e07cdcc7630d6068699ea781622323e`
- Download: https://github.com/blaxxun-boop/ServerSync/releases/download/v1.20/ServerSync.dll
- Size: 50,176 bytes
- SHA-256: `e8b61f621cc396c529d9194f202e0b22c6591dd919363a4501a579f9f7252e0e`
- Assembly/file version: `1.0.0.0` (upstream release version is `1.20`)
- License: upstream MIT-0, retained in `ServerSync.LICENSE.txt`

Valheim 1.0 made `ZRoutedRpc.Everybody` a literal constant. The previous bundled
binary attempted to read it as a runtime field, causing `MissingFieldException`
during configuration callbacks. Upstream v1.20 was recompiled for the new game
and no longer emits those field reads. This upgrade preserves the previous
public API and assembly identity.

To reproduce the dependency update, download the asset above, verify its size
and SHA-256, and copy the verified DLL to both paths. Do not substitute another
release merely because its assembly version also says `1.0.0.0`.
