using UnityEngine;
using UnityEngine.Rendering;
using WeaponFactory.Logic;
using WeaponFactory.Utilities;

namespace WeaponFactory.Levels
{
    /// <summary>Colour palette for one of the three environment themes.</summary>
    public class ThemePalette
    {
        public Color floor, floorLine, wall, wallTrim, accent, accent2, light, ambientSky, ambientGround, fog, background, machine, pipe;
        public string[] signs;

        public static ThemePalette For(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.TechLaboratory:
                    return new ThemePalette
                    {
                        floor = new Color(0.78f, 0.82f, 0.88f), floorLine = new Color(0.55f, 0.62f, 0.7f),
                        wall = new Color(0.72f, 0.77f, 0.84f), wallTrim = new Color(0.32f, 0.36f, 0.44f),
                        accent = new Color(0.1f, 0.85f, 1f), accent2 = new Color(0.45f, 1f, 0.7f),
                        light = new Color(0.95f, 0.98f, 1f), ambientSky = new Color(0.7f, 0.78f, 0.9f), ambientGround = new Color(0.4f, 0.45f, 0.52f),
                        fog = new Color(0.6f, 0.7f, 0.8f), background = new Color(0.62f, 0.72f, 0.82f),
                        machine = new Color(0.86f, 0.89f, 0.93f), pipe = new Color(0.5f, 0.6f, 0.7f),
                        signs = new[] { "LAB 02", "TEST ZONE", "R&D", "PROTOTYPE BAY", "CLEAN ROOM" },
                    };
                case EnvironmentTheme.RobotFactory:
                    return new ThemePalette
                    {
                        floor = new Color(0.22f, 0.2f, 0.3f), floorLine = new Color(0.4f, 0.3f, 0.6f),
                        wall = new Color(0.14f, 0.12f, 0.2f), wallTrim = new Color(0.08f, 0.07f, 0.12f),
                        accent = new Color(1f, 0.2f, 0.8f), accent2 = new Color(0.2f, 0.9f, 1f),
                        light = new Color(0.9f, 0.8f, 1f), ambientSky = new Color(0.4f, 0.3f, 0.6f), ambientGround = new Color(0.15f, 0.1f, 0.2f),
                        fog = new Color(0.1f, 0.07f, 0.16f), background = new Color(0.08f, 0.06f, 0.14f),
                        machine = new Color(0.3f, 0.27f, 0.4f), pipe = new Color(0.35f, 0.3f, 0.45f),
                        signs = new[] { "ASSEMBLY 7", "ROBOT WORKS", "CORE ACCESS", "SECTOR X", "AUTOMATON LINE" },
                    };
                default:
                    return new ThemePalette
                    {
                        floor = new Color(0.45f, 0.46f, 0.5f), floorLine = new Color(0.3f, 0.31f, 0.34f),
                        wall = new Color(0.36f, 0.3f, 0.27f), wallTrim = new Color(0.2f, 0.18f, 0.17f),
                        accent = new Color(1f, 0.55f, 0.1f), accent2 = new Color(1f, 0.85f, 0.2f),
                        light = new Color(1f, 0.93f, 0.82f), ambientSky = new Color(0.62f, 0.56f, 0.5f), ambientGround = new Color(0.25f, 0.22f, 0.2f),
                        fog = new Color(0.22f, 0.19f, 0.2f), background = new Color(0.2f, 0.17f, 0.18f),
                        machine = new Color(0.55f, 0.5f, 0.42f), pipe = new Color(0.6f, 0.35f, 0.2f),
                        signs = new[] { "WEAPON FACTORY", "ZONE A-1", "DANGER", "STAMPING PLANT", "SHIFT 03" },
                    };
            }
        }
    }

    /// <summary>
    /// Builds the factory environment from primitives: metal floor plates, conveyor belts, walls with
    /// glowing trims, pipes, machinery with animated gears/pistons/robot arms, rotating warning lights,
    /// signs, mechanical doors and the final combat arena. Static parts are static-batched.
    /// </summary>
    public static class EnvironmentBuilder
    {
        public const float TrackHalfWidth = 4.5f;
        public const float ArenaHalfWidth = 6.5f;

        public class Result
        {
            public Transform root;
            public Transform startDoorLeft, startDoorRight;
            public Transform arenaDoorLeft, arenaDoorRight;
            public Light sun;
        }

        public static void ApplyLighting(ThemePalette pal, out Light sun)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = pal.ambientSky;
            RenderSettings.ambientEquatorColor = Color.Lerp(pal.ambientSky, pal.ambientGround, 0.5f);
            RenderSettings.ambientGroundColor = pal.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = pal.fog;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 130f;
            RenderSettings.skybox = null;

            var lightGo = new GameObject("Sun");
            sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = pal.light;
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.55f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        }

        public static Result BuildLevel(EnvironmentTheme theme, float trackLength, float arenaLength, int seed)
        {
            var pal = ThemePalette.For(theme);
            var res = new Result();
            ApplyLighting(pal, out res.sun);
            var root = new GameObject("Environment").transform;
            res.root = root;
            var stat = Shapes.Empty(root, "Static").transform;
            var dyn = Shapes.Empty(root, "Dynamic").transform;
            var rng = new System.Random(seed);

            var floorMat = GameAssets.Textured(GameAssets.FloorGrid, pal.floor, new Vector2(3f, 6f));
            var edgeGlow = GameAssets.Glow(pal.accent, 1.8f);
            var wallMat = GameAssets.Lit(pal.wall, 0.3f, 0.2f);
            var trimMat = GameAssets.Lit(pal.wallTrim, 0.5f, 0.5f);
            var beltMatL = GameAssets.Textured(GameAssets.ConveyorStripes, Color.Lerp(pal.floor, Color.black, 0.45f), new Vector2(1f, 5f));
            var beltMatR = GameAssets.Textured(GameAssets.ConveyorStripes, Color.Lerp(pal.floor, Color.black, 0.45f), new Vector2(1f, 5f));
            var pipeMat = GameAssets.Lit(pal.pipe, 0.65f, 0.7f);

            // Lead-in area behind the start
            float startZ = -20f;
            float endZ = trackLength;
            const float seg = 20f;
            for (float z = startZ; z < endZ; z += seg)
            {
                float len = Mathf.Min(seg, endZ - z);
                float cz = z + len * 0.5f;
                // track floor
                var floor = Shapes.Box(stat, new Vector3(0f, -0.2f, cz), new Vector3(TrackHalfWidth * 2f, 0.4f, len), floorMat, "Floor");
                floor.GetComponent<MeshRenderer>().receiveShadows = true;
                // lane lines
                Shapes.Box(stat, new Vector3(-1.5f, 0.005f, cz), new Vector3(0.06f, 0.01f, len), GameAssets.Lit(pal.floorLine), "Lane");
                Shapes.Box(stat, new Vector3(1.5f, 0.005f, cz), new Vector3(0.06f, 0.01f, len), GameAssets.Lit(pal.floorLine), "Lane");
                // glowing edges + hazard curbs
                Shapes.Box(stat, new Vector3(-TrackHalfWidth - 0.15f, 0.12f, cz), new Vector3(0.3f, 0.24f, len), GameAssets.HazardMaterial, "CurbL");
                Shapes.Box(stat, new Vector3(TrackHalfWidth + 0.15f, 0.12f, cz), new Vector3(0.3f, 0.24f, len), GameAssets.HazardMaterial, "CurbR");
                Shapes.Box(stat, new Vector3(-TrackHalfWidth + 0.05f, 0.02f, cz), new Vector3(0.1f, 0.02f, len), edgeGlow, "EdgeL");
                Shapes.Box(stat, new Vector3(TrackHalfWidth - 0.05f, 0.02f, cz), new Vector3(0.1f, 0.02f, len), edgeGlow, "EdgeR");
                // side conveyors (lower, outside the track) — scrolling texture
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * (TrackHalfWidth + 1.6f);
                    var belt = Shapes.Box(dyn, new Vector3(x, -0.35f, cz), new Vector3(2.4f, 0.3f, len), side > 0 ? beltMatR : beltMatL, "Conveyor");
                    belt.AddComponent<TextureScroller>().speed = new Vector2(0f, side > 0 ? -0.5f : 0.5f);
                    Shapes.Box(stat, new Vector3(x - 1.3f, -0.3f, cz), new Vector3(0.15f, 0.4f, len), trimMat, "BeltRail");
                    Shapes.Box(stat, new Vector3(x + 1.3f, -0.3f, cz), new Vector3(0.15f, 0.4f, len), trimMat, "BeltRail");
                    // walls
                    float wx = side * 10.5f;
                    Shapes.Box(stat, new Vector3(wx, 4f, cz), new Vector3(1f, 9f, len), wallMat, "Wall");
                    Shapes.Box(stat, new Vector3(wx - side * 0.52f, 1.2f, cz), new Vector3(0.05f, 0.15f, len), GameAssets.Glow(pal.accent2, 1.4f), "WallTrimGlow");
                    Shapes.Box(stat, new Vector3(wx - side * 0.52f, 6.5f, cz), new Vector3(0.05f, 0.3f, len), trimMat, "WallTrim");
                    // pipes along the wall
                    Shapes.CylinderZ(stat, new Vector3(wx - side * 0.8f, 3.2f, cz), 0.45f, len, pipeMat, "Pipe");
                    Shapes.CylinderZ(stat, new Vector3(wx - side * 0.9f, 4.2f, cz), 0.25f, len, GameAssets.Lit(pal.accent * 0.7f), "PipeSmall");
                    // floor beside the belts
                    Shapes.Box(stat, new Vector3(side * 8.3f, -0.45f, cz), new Vector3(3.5f, 0.3f, len), GameAssets.Lit(pal.wallTrim), "SideFloor");
                }
                // pipe clamps and vertical pipes
                for (float pz = z + 4f; pz < z + len; pz += 10f)
                {
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Shapes.CylinderZ(stat, new Vector3(side * 9.7f, 3.2f, pz), 0.6f, 0.25f, trimMat, "Clamp");
                        Shapes.CylinderY(stat, new Vector3(side * 9.4f, 4f, pz + 5f), 0.3f, 8f, pipeMat, "VPipe");
                    }
                }
            }

            // machinery, lights, signs and crates riding the conveyors
            int signIndex = 0;
            for (float z = 6f; z < endZ - 6f; z += 12f + (float)rng.NextDouble() * 6f)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                BuildMachine(stat, dyn, new Vector3(side * 8.5f, 0f, z), pal, rng);
                // crate on conveyor
                var crate = Shapes.Box(dyn, new Vector3(-side * (TrackHalfWidth + 1.6f), 0.15f, z + 3f), new Vector3(1.0f, 0.7f, 1.0f),
                    GameAssets.Lit(Color.Lerp(pal.machine, pal.accent, 0.35f), 0.3f, 0.2f), "Crate");
                var mover = crate.AddComponent<PingPongMover>();
                mover.offset = new Vector3(0f, 0f, 3.5f);
                mover.period = 5f + (float)rng.NextDouble() * 3f;
                // warning beacon on the wall
                BuildBeacon(dyn, new Vector3(side * 9.9f, 6f, z + 6f), pal.accent);
                if (rng.NextDouble() < 0.45)
                {
                    string text = pal.signs[signIndex++ % pal.signs.Length];
                    BuildSign(stat, new Vector3(-side * 9.95f, 5.2f, z + 4f), text, pal, side > 0 ? -90f : 90f); // face the track
                }
            }

            // overhead girders (also make the space read as indoor factory)
            for (float z = 0f; z < endZ - 24f; z += 16f) // none near the arena: the arena camera sits higher
            {
                Shapes.Box(stat, new Vector3(0f, 9f, z), new Vector3(21f, 0.5f, 0.6f), trimMat, "Girder");
                Shapes.Box(stat, new Vector3(0f, 8.6f, z), new Vector3(3f, 0.25f, 0.4f), GameAssets.Glow(pal.light, 1.4f), "Lamp");
            }

            // start door (opens when the run starts)
            BuildDoor(stat, new Vector3(0f, 0f, 5f), pal, out res.startDoorLeft, out res.startDoorRight, "START");

            BuildArena(stat, dyn, trackLength, arenaLength, pal, res);

            // Background backdrop beyond the fog
            Shapes.Box(stat, new Vector3(0f, 10f, trackLength + arenaLength + 12f), new Vector3(60f, 30f, 1f), GameAssets.Lit(pal.background * 0.8f), "Backdrop");

            TryStaticBatch(stat.gameObject);
            return res;
        }

        static void BuildMachine(Transform stat, Transform dyn, Vector3 pos, ThemePalette pal, System.Random rng)
        {
            var machineMat = GameAssets.Lit(pal.machine, 0.45f, 0.4f);
            var dark = GameAssets.Lit(pal.wallTrim, 0.5f, 0.5f);
            var glow = GameAssets.Glow(pal.accent, 2f);
            int kind = rng.Next(4);
            float side = Mathf.Sign(pos.x);
            switch (kind)
            {
                case 0: // stamping press with piston
                    {
                        Shapes.Box(stat, pos + new Vector3(0f, 1.5f, 0f), new Vector3(2.4f, 3f, 2.4f), machineMat, "Press", true);
                        Shapes.Box(stat, pos + new Vector3(-side * 1.21f, 1.8f, 0f), new Vector3(0.05f, 0.6f, 1.6f), glow, "PressPanel");
                        var piston = Shapes.Box(dyn, pos + new Vector3(0f, 3.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), dark, "Piston");
                        var m = piston.AddComponent<PingPongMover>();
                        m.offset = new Vector3(0f, 0.6f, 0f);
                        m.period = 1.2f + (float)rng.NextDouble();
                        break;
                    }
                case 1: // gear tower
                    {
                        Shapes.Box(stat, pos + new Vector3(0f, 2f, 0f), new Vector3(1.6f, 4f, 1.6f), dark, "Tower", true);
                        for (int i = 0; i < 2; i++)
                        {
                            var gear = Shapes.Empty(dyn, "Gear", pos + new Vector3(-side * 0.85f, 1.5f + i * 1.6f, 0f)).transform;
                            gear.rotation = Quaternion.Euler(0f, 0f, 90f);
                            var disc = Shapes.CylinderY(gear, Vector3.zero, 1.3f - i * 0.3f, 0.2f, machineMat, "Disc");
                            for (int k = 0; k < 8; k++)
                            {
                                float a = k * Mathf.PI / 4f;
                                Shapes.Box(disc.transform, new Vector3(Mathf.Cos(a) * 0.55f, 0f, Mathf.Sin(a) * 0.55f), new Vector3(0.18f, 1f, 0.18f), machineMat, "Tooth");
                            }
                            Shapes.CylinderY(gear, Vector3.zero, 0.35f, 0.25f, glow, "Hub");
                            var sp = gear.gameObject.AddComponent<Spinner>();
                            sp.axis = Vector3.up;
                            sp.speed = (i == 0 ? 60f : -90f);
                        }
                        break;
                    }
                case 2: // robotic assembly arm
                    {
                        Shapes.CylinderY(stat, pos + new Vector3(0f, 0.4f, 0f), 1.4f, 0.8f, dark, "ArmBase");
                        var yaw = Shapes.Empty(dyn, "ArmYaw", pos + new Vector3(0f, 0.8f, 0f)).transform;
                        var sp = yaw.gameObject.AddComponent<Spinner>();
                        sp.axis = Vector3.up;
                        sp.speed = 35f + (float)rng.NextDouble() * 30f;
                        Shapes.Box(yaw, new Vector3(0f, 1.1f, 0f), new Vector3(0.4f, 2.2f, 0.4f), machineMat, "Arm1");
                        var a2 = Shapes.Box(yaw, new Vector3(0.7f, 2.3f, 0f), new Vector3(1.6f, 0.3f, 0.3f), machineMat, "Arm2");
                        a2.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                        Shapes.Sphere(yaw, new Vector3(1.45f, 2.0f, 0f), 0.35f, glow, "Tool");
                        break;
                    }
                default: // tank with gauges / moving platform
                    {
                        Shapes.CylinderY(stat, pos + new Vector3(0f, 1.7f, 0f), 2.2f, 3.4f, GameAssets.Lit(pal.pipe, 0.7f, 0.7f), "Tank", true);
                        Shapes.CylinderY(stat, pos + new Vector3(0f, 2.6f, 0f), 2.28f, 0.2f, glow, "TankBand");
                        var platform = Shapes.Box(dyn, pos + new Vector3(0f, 0.5f, 2.2f), new Vector3(1.8f, 0.2f, 1.4f), GameAssets.HazardMaterial, "Platform");
                        var mv = platform.AddComponent<PingPongMover>();
                        mv.offset = new Vector3(0f, 1.6f, 0f);
                        mv.period = 3f;
                        break;
                    }
            }
        }

        static void BuildBeacon(Transform dyn, Vector3 pos, Color color)
        {
            Shapes.CylinderY(dyn, pos + Vector3.down * 0.25f, 0.4f, 0.2f, GameAssets.Lit(new Color(0.15f, 0.15f, 0.17f)), "BeaconBase");
            var rot = Shapes.Empty(dyn, "Beacon", pos).transform;
            Shapes.Sphere(rot, Vector3.zero, 0.35f, GameAssets.Glow(color, 2.5f), "Bulb");
            Shapes.Box(rot, new Vector3(0.4f, 0f, 0f), new Vector3(0.6f, 0.12f, 0.12f), GameAssets.Glow(color, 3f), "Beam");
            var sp = rot.gameObject.AddComponent<Spinner>();
            sp.axis = Vector3.up;
            sp.speed = 240f;
        }

        static void BuildSign(Transform stat, Vector3 pos, string text, ThemePalette pal, float yaw)
        {
            var sign = Shapes.Empty(stat, "Sign_" + text, pos).transform;
            sign.rotation = Quaternion.Euler(0f, yaw, 0f);
            Shapes.Box(sign, Vector3.zero, new Vector3(4.2f, 1.1f, 0.12f), GameAssets.Lit(pal.wallTrim), "Board");
            Shapes.Box(sign, new Vector3(0f, -0.58f, -0.02f), new Vector3(4.2f, 0.06f, 0.12f), GameAssets.Glow(pal.accent, 2f), "Underline");
            Shapes.Text3D(sign, text, new Vector3(0f, 0f, -0.08f), 0.06f, pal.accent2);
        }

        static void BuildDoor(Transform stat, Vector3 pos, ThemePalette pal, out Transform left, out Transform right, string label)
        {
            var frame = GameAssets.Lit(pal.wallTrim, 0.5f, 0.6f);
            Shapes.Box(stat, pos + new Vector3(-TrackHalfWidth - 0.6f, 2.6f, 0f), new Vector3(1.2f, 5.2f, 1f), frame, "DoorFrameL", true);
            Shapes.Box(stat, pos + new Vector3(TrackHalfWidth + 0.6f, 2.6f, 0f), new Vector3(1.2f, 5.2f, 1f), frame, "DoorFrameR", true);
            Shapes.Box(stat, pos + new Vector3(0f, 5.5f, 0f), new Vector3(TrackHalfWidth * 2f + 2.4f, 0.8f, 1f), frame, "DoorFrameTop", true);
            Shapes.Text3D(stat, label, pos + new Vector3(0f, 5.5f, -0.55f), 0.08f, pal.accent2);
            var parent = stat.parent; // doors must move -> not static batched
            var doorMat = GameAssets.Lit(Color.Lerp(pal.machine, pal.wallTrim, 0.4f), 0.5f, 0.6f);
            left = Shapes.Empty(parent, "DoorL", pos + new Vector3(-TrackHalfWidth * 0.5f, 0f, 0f)).transform;
            right = Shapes.Empty(parent, "DoorR", pos + new Vector3(TrackHalfWidth * 0.5f, 0f, 0f)).transform;
            foreach (var d in new[] { left, right })
            {
                Shapes.Box(d, new Vector3(0f, 2.5f, 0f), new Vector3(TrackHalfWidth, 5f, 0.4f), doorMat, "Panel", true);
                Shapes.Box(d, new Vector3(0f, 2.5f, -0.21f), new Vector3(TrackHalfWidth - 0.4f, 0.5f, 0.02f), GameAssets.HazardMaterial, "Stripe");
                Shapes.Box(d, new Vector3(0f, 4.2f, -0.21f), new Vector3(0.8f, 0.3f, 0.02f), GameAssets.Glow(pal.accent, 2f), "Light");
            }
        }

        public static void OpenDoor(Transform left, Transform right, float duration = 0.8f)
        {
            if (left != null) Tween.Move(left, left.position + Vector3.left * (TrackHalfWidth + 0.2f), duration, Ease.InOutQuad);
            if (right != null) Tween.Move(right, right.position + Vector3.right * (TrackHalfWidth + 0.2f), duration, Ease.InOutQuad);
        }

        static void BuildArena(Transform stat, Transform dyn, float startZ, float length, ThemePalette pal, Result res)
        {
            float cz = startZ + length * 0.5f;
            var floorMat = GameAssets.Textured(GameAssets.FloorGrid, Color.Lerp(pal.floor, pal.wallTrim, 0.3f), new Vector2(5f, 10f));
            Shapes.Box(stat, new Vector3(0f, -0.2f, cz), new Vector3(ArenaHalfWidth * 2f + 4f, 0.4f, length), floorMat, "ArenaFloor");
            // glowing target rings on the floor
            for (int i = 0; i < 3; i++)
            {
                Shapes.CylinderY(stat, new Vector3(0f, 0.01f + i * 0.002f, startZ + length * 0.6f), 11f - i * 3.5f, 0.01f, GameAssets.Glow(i % 2 == 0 ? pal.accent : pal.accent2, 0.9f), "ArenaRing");
                Shapes.CylinderY(stat, new Vector3(0f, 0.012f + i * 0.002f, startZ + length * 0.6f), 10.6f - i * 3.5f, 0.01f, GameAssets.Lit(Color.Lerp(pal.floor, pal.wallTrim, 0.3f)), "ArenaRingInner");
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float wx = side * (ArenaHalfWidth + 3f);
                Shapes.Box(stat, new Vector3(wx, 4f, cz), new Vector3(1f, 9f, length), GameAssets.Lit(pal.wall), "ArenaWall");
                for (float z = startZ + 3f; z < startZ + length; z += 6f)
                {
                    Shapes.Box(stat, new Vector3(wx - side * 0.55f, 3f, z), new Vector3(0.1f, 4f, 0.4f), GameAssets.Glow(pal.accent, 1.8f), "ArenaLight");
                    BuildBeacon(dyn, new Vector3(wx - side * 0.7f, 7f, z + 3f), pal.accent2);
                }
                // railings at the edges of the combat area
                Shapes.Box(stat, new Vector3(side * (ArenaHalfWidth + 0.5f), 0.5f, cz), new Vector3(0.15f, 1f, length), GameAssets.HazardMaterial, "Rail");
            }
            // back wall + big door the robots come from
            float backZ = startZ + length;
            Shapes.Box(stat, new Vector3(0f, 4f, backZ), new Vector3(ArenaHalfWidth * 2f + 8f, 9f, 1f), GameAssets.Lit(pal.wall), "ArenaBack");
            Shapes.Box(stat, new Vector3(0f, 7.6f, backZ - 0.6f), new Vector3(8f, 1.2f, 0.2f), GameAssets.Lit(pal.wallTrim), "ArenaSignBoard");
            Shapes.Text3D(stat, "COMBAT ZONE", new Vector3(0f, 7.6f, backZ - 0.75f), 0.12f, pal.accent);
            var parent = stat.parent;
            res.arenaDoorLeft = Shapes.Empty(parent, "ArenaDoorL", new Vector3(-2f, 0f, backZ - 0.6f)).transform;
            res.arenaDoorRight = Shapes.Empty(parent, "ArenaDoorR", new Vector3(2f, 0f, backZ - 0.6f)).transform;
            foreach (var d in new[] { res.arenaDoorLeft, res.arenaDoorRight })
            {
                Shapes.Box(d, new Vector3(0f, 3f, 0f), new Vector3(4f, 6f, 0.3f), GameAssets.Lit(pal.machine, 0.5f, 0.6f), "Panel", true);
                Shapes.Box(d, new Vector3(0f, 3f, -0.16f), new Vector3(3.6f, 0.4f, 0.02f), GameAssets.HazardMaterial, "Stripe");
            }
            // arena entrance arch
            Shapes.Box(stat, new Vector3(-ArenaHalfWidth - 1.5f, 3f, startZ), new Vector3(1.4f, 6f, 1.2f), GameAssets.Lit(pal.wallTrim), "ArchL", true);
            Shapes.Box(stat, new Vector3(ArenaHalfWidth + 1.5f, 3f, startZ), new Vector3(1.4f, 6f, 1.2f), GameAssets.Lit(pal.wallTrim), "ArchR", true);
            // (no top beam: it would block the arena camera's view)
            Shapes.Box(stat, new Vector3(-ArenaHalfWidth - 0.78f, 3f, startZ), new Vector3(0.05f, 5.6f, 0.3f), GameAssets.Glow(pal.accent, 2.2f), "ArchGlowL");
            Shapes.Box(stat, new Vector3(ArenaHalfWidth + 0.78f, 3f, startZ), new Vector3(0.05f, 5.6f, 0.3f), GameAssets.Glow(pal.accent, 2.2f), "ArchGlowR");
        }

        /// <summary>Main menu backdrop: a short section of factory with machinery, conveyors and lights.</summary>
        public static ThemePalette BuildMenuBackdrop(EnvironmentTheme theme, out Light sun)
        {
            var pal = ThemePalette.For(theme);
            ApplyLighting(pal, out sun);
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 60f;
            var root = new GameObject("MenuEnvironment").transform;
            var stat = Shapes.Empty(root, "Static").transform;
            var dyn = Shapes.Empty(root, "Dynamic").transform;
            var rng = new System.Random(7);
            var floorMat = GameAssets.Textured(GameAssets.FloorGrid, pal.floor, new Vector2(6f, 6f));
            Shapes.Box(stat, new Vector3(0f, -0.2f, 10f), new Vector3(30f, 0.4f, 40f), floorMat, "Floor");
            var beltMat = GameAssets.Textured(GameAssets.ConveyorStripes, Color.Lerp(pal.floor, Color.black, 0.45f), new Vector2(1f, 8f));
            var belt = Shapes.Box(dyn, new Vector3(0f, 0.1f, 8f), new Vector3(3f, 0.3f, 30f), beltMat, "Conveyor");
            belt.AddComponent<TextureScroller>().speed = new Vector2(0f, -0.4f);
            Shapes.Box(stat, new Vector3(-1.6f, 0.15f, 8f), new Vector3(0.2f, 0.45f, 30f), GameAssets.HazardMaterial, "BeltRail");
            Shapes.Box(stat, new Vector3(1.6f, 0.15f, 8f), new Vector3(0.2f, 0.45f, 30f), GameAssets.HazardMaterial, "BeltRail");
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                BuildMachine(stat, dyn, new Vector3(side * (5f + (float)rng.NextDouble() * 2f), 0f, 4f + i * 4f), pal, rng);
            }
            for (int i = 0; i < 4; i++)
            {
                var crate = Shapes.Box(dyn, new Vector3(0f, 0.6f, 4f + i * 6f), new Vector3(1.1f, 0.8f, 1.1f), GameAssets.Lit(Color.Lerp(pal.machine, pal.accent, 0.35f)), "Crate");
                var mv = crate.AddComponent<PingPongMover>();
                mv.offset = new Vector3(0f, 0f, 2.5f);
                mv.period = 6f;
                mv.phase = i;
            }
            Shapes.Box(stat, new Vector3(0f, 5f, 26f), new Vector3(30f, 12f, 1f), GameAssets.Lit(pal.wall), "BackWall");
            BuildSign(stat, new Vector3(0f, 6.5f, 25.4f), "WEAPON FACTORY", pal, 0f);
            for (int i = -2; i <= 2; i++) BuildBeacon(dyn, new Vector3(i * 5f, 9f, 25f), pal.accent);
            TryStaticBatch(stat.gameObject);
            return pal;
        }

        static void TryStaticBatch(GameObject root)
        {
            try { StaticBatchingUtility.Combine(root); }
            catch (System.Exception e) { Debug.LogWarning("[Environment] Static batching skipped: " + e.Message); }
        }
    }
}
