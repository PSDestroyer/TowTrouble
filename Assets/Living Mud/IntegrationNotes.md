# Tow Trouble integration

Imported from the local Living Mud URP 1.0 package documented in TOW_TROUBLE.md. This is the core DMS/Mud03/tire-stamp dependency subset; original GUIDs are preserved. Original demo scenes, VFX graphs and Packages/manifest.json were excluded.

Compatibility hook: DynamicMudSurface.RefreshReferences calls the original SetReferences after the integration assigns an isolated session material. The authored component is disabled so its legacy Escape input does not run; it still provides the vendor surface references and metadata. Two explicit assembly definitions isolate vendor and game integration code. The tread texture uses linear (sRGB disabled) import because its RGB channels contain normal data; alpha transparency remains enabled.

Game-specific meshes/materials are under Assets/TowTrouble/Art/Mud. Runtime integration and batched GPU compositing are under Assets/TowTrouble/Integrations/LivingMud. No vendor material or texture is changed during gameplay. Physics and outcomes remain in RecoveryWorld/RecoveryVehicle.

The native Texture with Mud Shader Graph is also imported for tire coating. Derived wheel materials are game-owned and preserve the original clean texture/tint; the vehicle body is excluded. DMS Flip UV is set to 1 to prevent its default 180-degree stamp-space rotation with the tutorial mesh.
