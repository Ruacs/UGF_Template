# HexaAway Sample Package

This folder owns HexaAway-specific gameplay scripts, prefabs, materials, textures, and ScriptableObject assets.

## Folder Map

- `Scripts/`: HexaAway runtime code. Keep concrete gameplay code here.
- `Config/`: HexaAway JSON, bytes, or other custom runtime configs.
- `Config/CompactLevels.json`: compact level library. Runtime parses rows into `RuntimeLevelData`.
- `ScriptableObjects/Database/`: `ElementRegistry` and database-style index assets.
- `ScriptableObjects/Visual/`: `TileVisualSkinDataSO` assets. Each skin owns tile prefab, direction visual, and visual group mapping.
- `Prefabs/`: HexaAway gameplay prefabs, following the existing project convention.
- `Entity/`: GF entity prefabs if HexaAway later uses the Entity system.
- `Mesh/`: HexaAway mesh assets.
- `Material/`: HexaAway gameplay materials.
- `Texture/`: HexaAway gameplay textures.
- `UI/`: HexaAway-specific UI prefabs or UI-only assets if the project keeps them in the subgame domain.

Scene placeholders belong to:

- `Assets/GameMain/Scenes/SubGame/HexaAway/`

## Migration Rules

- Do not copy the event system from `TapOut_HexaAway`.
- Prefer existing GF lifecycle, event, UI, resource, and procedure mechanisms.
- Old project events are reference material for gameplay intent only.
- If there is no clear GF equivalent for an event, stop and confirm before adding a new event ID or event class.
- Do not introduce a new global singleton or standalone event bus for this migration.

## Runtime assets

The sample is already integrated with GF Procedure/UI. Its authored assets include:

- `Config/CompactLevels.json`
- `ScriptableObjects/Database/ElementRegistry.asset`
- `ScriptableObjects/Database/TilesVisualsData.asset`
- `ScriptableObjects/Visual/TileVisualSkin_Default.asset`
- `Prefabs/Platforms/Platform.prefab`
- `Prefabs/HexaBlock.prefab`
- Basic tile visual materials under `Material/`

## Start and test

1. Open `Assets/GameMain/Scenes/GameLauncher.unity`.
2. Verify HexaAway is listed on `GameEntry/GameFramework/GameManager` and set it as the primary game if desired.
3. Enter Play Mode and start the primary game from the menu.
4. Use the game's `Scripts/TestMode/` modules for diagnostics; their lifetime follows the game Procedure. Test mode must first be enabled using the template's existing controls.

`LocalTestBootstrap` is a legacy scene helper, not a replacement for GF startup. The visual test scene now belongs to `Assets/GameMain/Scenes/SubGame/HexaAway/Test.unity`; it is not the product entry scene.

## Tile Motion And Selection

- One `TileBehavior` remains one logical tile, regardless of the number of rendered pieces.
- `TileMovement.Execute(TileMotionTask)` owns the complete visual route. `TileBehavior` owns clicks, move cost, occupancy and final collection.
- `Default` uses `Prefabs/HexaTile_Piece.prefab`; `Single` uses the existing `Prefabs/HexaBlock.prefab` and Animator.
- Call `GameEntry.HexaAway.SelectTileSkin("Default")` or `SelectTileSkin("Single")` from the selection UI. Selection is saved and takes effect on the next level build/restart, not halfway through a move. The installed sample owns `UI/SelectionUIPanel.prefab`.
- Each prefab must explicitly contain `TileBehavior`, one `TileMovement`, and one `TileVisuals`. Missing components produce an error instead of silently adding a single-tile implementation.
- Three-piece timing is on `PieceTileMovement`: step 0.15s, follower delay 0.13s, return 0.4s, return arc 0.8. Meshes are read for support calculations; no mesh or normals are generated at runtime.
- A blocked move appends a direct airborne return for each piece. Departure order becomes bottom-to-top arrival order, and the resulting stack is used on the next click.
- A hole remains the next empty cell in the route. Pieces reach that cell before falling.
- Saw declares `ConsumesPieces`, plays the referenced break particle per `OnPieceArrived`, and leaves logical collection to the task owner after all pieces finish. Bomb declares `TriggerOnFirstPiece` and `CollectsTileOnArrival`: the leader triggers the explosion and the entire arriving tile is collected immediately, cancelling its followers. Ordinary terminal interactions execute `OnTileStepped` once after the stack arrives.
- Route events are deduplicated per step index; turns and wormholes are executed separately by each piece. This also lets followers traverse a snapshotted route after a collapse platform is removed by the leader.
- A guaranteed collection (hole, Saw, Bomb) releases its origin immediately, allowing other tiles to move concurrently. Returns and ordinary stops retain their origin. Stopped destinations are reserved, except reusable per-piece consumers such as Saw. Collection only removes grid entries owned by that tile.
- Reverse and SquareReverse expose `BlocksTileInput` for the entire exchange. Both the manager and direct tile clicks check this lock. Completion checks also wait for the exchange; completing or interrupting its sequence releases the lock after restoring consistent board indices.
- `StopMovement` cancels all pieces and restores the captured pose for a live tile. Collection cancels without restoring. The manager defers win/failure checks until remaining moving tiles complete.
- EditMode regression coverage: `Lokas.Editor.TileMotionTests` checks route snapshots, callback idempotency/cancellation and six-direction return/contact against the authored prefab.

## Package integration

- `Scripts/HexaAwayGameManagerComponent.Module.cs` supplies the runtime entry, save registration and optional capabilities. The core does not instantiate HexaAway types.
- `Scripts/Config/HexaAwayServerConfig.cs` owns game-specific server settings; the seven common feature/timer settings remain in `AdsServerConfig.Common`.
- `ScriptableObjects/Registry/HexaAwayAssetRegistryConfig.asset` owns runtime resource keys.
- `Editor/PackageManifest.json` owns installation registrations. UI IDs 216/217 and scene ID 4 remain stable.
- Use `Tools/Template/Sample SubGames` to export, remove or import. Keep the exported `.unitypackage` and adjacent `.json` together. Do not delete this folder manually.
- This is an extension for the compatible GF template, not a standalone Unity project. Shared audio, plugins and template code are deliberately not duplicated in the package.
