using System.Collections.Generic;
using GoatDescent.Levels;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    internal static class TerracePeakMesh
    {
        private static readonly Color Rock = new Color(0f, 0f, 0f, 1f);
        private static readonly float Tau = Mathf.PI * 2f;
        private const int Sides = 128;
        private const int TierCount = 8;
        private const int CaveTier = 4;
        private const int MainSide = 96;
        private const int SecondSide = 43;
        public const int TunnelExitSide = 32;
        private const float RadScale = 1.15f;
        private const float HeightScale = 1.12f;
        private const int TerraceSides = 144;
        private const int Grooves = 12;
        private const float CaveCeilingU = .6f;
        public const float PeakHeight = 440f;
        public const float WaterLevel = -4.5f;

        private static readonly float[] BaseY = { 0f, 60f, 98f, 172f, 222f, 284f, 312f, 368f };
        private static readonly float[] TopY = { 60f, 98f, 172f, 222f, 284f, 312f, 368f, 400f };
        private static readonly float[] WallY = { 52f, 92f, 162f, 214f, 276f, 306f, 364f, 396f };
        private static readonly float[] TierR = { 303f, 285f, 272f, 241f, 213f, 189f, 176f, 147f };
        private static readonly float[] Taper = { 1.8f, 1.8f, 2.7f, 1.8f, 2.25f, 1.8f, 2.7f, 2.25f };
        private static readonly int[] WideShelf = { 0, 0, 1, 1, 1, 0, 1, 1 };
        private const float SummitY = 400f;
        private const float SummitR = 121f;

        private static readonly float[] FacadeAngle = { .4f, 2.5f, 5.9f }; 
        private static readonly float[] FacadeHalf = { 4.5f, 5.9f, 3.7f };
        private static readonly float[] FacadeDepth = { 46f, 64f, 38f };
        private static readonly int[] FacadeFirst = { 1, 1, 2 };
        private static readonly int[] FacadeLast = { 4, 6, 5 };

        private static readonly float[] CutAngle = { 1.45f, 3.5f, 5.6f };
        private static readonly float[] BreachAngle = { 1.95f, 3.65f, .95f };
        private static readonly float[] BreachWidth = { .17f, .16f, .21f };
        private static readonly float[] BreachDepth = { 44f, 36f, 52f };
        private static readonly float[] BreachDrop = { 34f, 28f, 42f };
        private static readonly int[] BreachTier = { 2, 5, 6 };
        private static readonly int[] MainHalf = { 3, 3, 3, 3, 3 };
        private static readonly int[] MainUpperHalf = { 2, 2, 1, 0, 0 };
        private static readonly int[] SecondHalf = { 3, 3, 2, 1, 0 };

        private struct JumpStep
        {
            public float Angle;
            public float Y;
            public float Size;
            public float Width;
            public float Span;
        }

        private static readonly JumpStep[] JumpSteps =
        {
            new JumpStep { Angle = 20f, Y = 34f, Size = 3.2f, Width = 12f, Span = 1.4f },
            new JumpStep { Angle = 3f, Y = 42f, Size = 2.6f, Width = 10f, Span = 1.3f },
            new JumpStep { Angle = -12f, Y = 76f, Size = 3.4f, Width = 12f, Span = 1.4f },
            new JumpStep { Angle = 2f, Y = 88f, Size = 2.8f, Width = 9f, Span = 1.3f },
            new JumpStep { Angle = -28f, Y = 132f, Size = 3.4f, Width = 13f, Span = 1.4f },
            new JumpStep { Angle = -42f, Y = 120f, Size = 3f, Width = 10f, Span = 1.3f },
            new JumpStep { Angle = -52f, Y = 190f, Size = 3.4f, Width = 13f, Span = 1.4f },
            new JumpStep { Angle = -70f, Y = 202f, Size = 3f, Width = 10f, Span = 1.3f },
            new JumpStep { Angle = -138f, Y = 246f, Size = 3.4f, Width = 11f, Span = 1.4f },
            new JumpStep { Angle = -126f, Y = 260f, Size = 2.8f, Width = 9f, Span = 1.3f },
            new JumpStep { Angle = -114f, Y = 292f, Size = 3.3f, Width = 10f, Span = 1.4f },
            new JumpStep { Angle = -128f, Y = 306f, Size = 2.8f, Width = 9f, Span = 1.3f },
            new JumpStep { Angle = -62f, Y = 336f, Size = 3f, Width = 10f, Span = 1.4f },
            new JumpStep { Angle = -48f, Y = 348f, Size = 2.7f, Width = 9f, Span = 1.3f },
            new JumpStep { Angle = -12f, Y = 380f, Size = 2.8f, Width = 9f, Span = 1.4f },
            new JumpStep { Angle = 2f, Y = 392f, Size = 2.6f, Width = 8f, Span = 1.3f }
        };
        private static float shade = 1f;

        private static readonly float[] ValleyRings =
        {
            280f, 312f, 338f, 358f, 376f, 394f, 412f, 432f, 450f, 466f, 480f, 492f,
            502f, 512f, 521f, 530f, 540f, 551f, 564f, 579f, 596f, 616f, 640f, 668f,
            700f, 740f, 790f, 850f, 920f, 1000f, 1090f, 1180f, 1270f, 1360f, 1450f, 1540f,
            1630f, 1720f, 1810f, 1900f, 1990f, 2080f, 2170f, 2260f, 2350f, 2450f
        };

        private const float RiverAxis = .087f;
        private const float RiverOffset = -460f;
        private static readonly float[] ArmU = { 300f, -820f };
        private static readonly float[] ArmBend = { 1.05f, -1.2f };
        private static readonly float[] ArmLength = { 1650f, 1450f };
        private static readonly float[] ArmWidth = { 42f, 34f };
        private static readonly float[] BayU = { -540f, 180f, 880f };
        private static readonly float[] BaySize = { 66f, 58f, 62f };

        private struct Tree
        {
            public Vector3 Position;
            public float Size;
            public float Yaw;
        }

        private struct Ridge
        {
            public float Angle;
            public float Distance;
            public float Span;
            public float Height;
            public float Width;
            public float Twist;
        }

        private struct Mound
        {
            public float X;
            public float Z;
            public float Radius;
            public float Height;
        }

        private static Tree[] trees;
        private static Ridge[] ridges;
        private static Mound[] mounds;

        private static float SideAngle(int side) => side * Tau / Sides;

        private static float Shape(float angle) =>
            1f + .05f * Mathf.Sin(angle * 2f + .8f) + .035f * Mathf.Sin(angle * 3f - 1.2f)
            + .024f * Mathf.Sin(angle * 7f + 1.1f) + .013f * Mathf.Sin(angle * 13f - .7f)
            + .008f * Mathf.Sin(angle * 23f + 2.2f) + .006f * Mathf.Sin(angle * 31f - 2.6f)
            + .004f * Mathf.Sin(angle * 47f + .9f);

        private static float Hash(float n)
        {
            float s = Mathf.Sin(n) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        private static float Ragged(float angle, float seed, float bite)
        {
            float value = Mathf.PerlinNoise(angle * 4.3f + seed * 1.7f, seed * .9f) - .5f;
            value += .5f * (Mathf.PerlinNoise(angle * 9.7f + seed * 2.3f, seed * .6f + 3.1f) - .5f);
            float notch = Mathf.PerlinNoise(angle * 2.1f + seed * 3.7f, seed * 2.9f + 1.3f);
            value -= Mathf.Pow(Mathf.Max(0f, notch - .64f) / .36f, 2f) * bite;
            return value;
        }

        private static float RingNoise(float angle, float frequency, float seed) =>
            Mathf.PerlinNoise(Mathf.Cos(angle) * frequency + seed, Mathf.Sin(angle) * frequency + seed * 1.63f);

        private static float StepBulge(float angle, float nominal)
        {
            float degrees = angle * Mathf.Rad2Deg;
            float value = 0f;
            for (int step = 0; step < JumpSteps.Length; step++)
            {
                JumpStep jump = JumpSteps[step];
                float delta = Mathf.DeltaAngle(degrees, jump.Angle);
                float vertical = (nominal - jump.Y) / jump.Span;
                float grain = RingNoise(angle, 2.4f, step * 3.7f + 1.3f) - .5f;
                float weather = RingNoise(angle, 5.3f, step * 5.9f + 2.9f) - .5f;
                float grit = RingNoise(angle, 9.7f, step * 7.1f + 4.7f) - .5f;
                float lobe = Mathf.Clamp(.78f + .62f * grain + .3f * weather + .1f * grit, .42f, 1.3f);
                float widthFactor = Mathf.Lerp(1.36f, .78f, Mathf.Clamp01(vertical * .5f + .5f + .16f * grain))
                    + .26f * weather;
                float lean = Mathf.Clamp(.86f + .46f * grit, .62f, 1.1f);
                float lateral = delta / (jump.Width * widthFactor * (delta < 0f ? lean : 2f - lean));
                float teeth = 0f;
                for (int crest = 0; crest < 3; crest++)
                {
                    float place = -.6f + crest * .56f + .18f * (weather + grit);
                    float ridge = Mathf.Exp(-Mathf.Pow((delta / jump.Width - place) / .19f, 2f));
                    teeth += ridge * (.35f + .8f * RingNoise(angle, 6.4f, step * 2.3f + crest * 4.7f));
                }
                float topBand = Mathf.Exp(-Mathf.Pow((vertical - .62f) / .44f, 2f));
                value += jump.Size * lobe * Mathf.Exp(-(lateral * lateral + vertical * vertical))
                    * (1f + .42f * topBand * teeth);
            }
            return value;
        }

        private static float Asym(float angle, float y)
        {
            float amp = Mathf.Lerp(.1f, .3f, Mathf.Clamp01(y / 380f));
            return 1f + amp * (Mathf.Sin(angle - .8f) + .45f * Mathf.Sin(2f * angle + 2.2f)
                + .2f * Mathf.Sin(3f * angle - .4f));
        }

        private static float TiltAt(float y, float angle)
        {
            float amp = 58f * Mathf.Pow(Mathf.Clamp01(y / 400f), 1.2f);
            float phase = angle + y / 200f;
            return amp * (.7f * Mathf.Sin(phase + .6f) + .3f * Mathf.Sin(2f * phase - 1.1f));
        }

        private static float Flute(float angle, float height)
        {
            float groove = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(angle * Grooves + height * .002f)), 12f) * 26f;
            float jag = Mathf.Sin(angle * 33f + height * .01f) * 2.1f + Mathf.Sin(angle * 57f - height * .018f) * 1.2f;
            return -groove + jag;
        }

        private static float Cut(float angle, float nominal)
        {
            float weight = Mathf.Clamp01((nominal - 180f) / 200f) * 30f;
            float value = 0f;
            for (int cut = 0; cut < CutAngle.Length; cut++)
            {
                float offset = Mathf.Abs(AngleDelta(angle, CutAngle[cut]));
                value += weight * Mathf.Exp(-Mathf.Pow(offset / .085f, 2f));
            }
            return -value;
        }

        private static bool InSpan(int side, int center, int half) => half > 0 && side >= center - half && side < center + half;

        private static readonly float[] WallUsDetailed = { .2f, .4f, .6f, .78f, 1f };
        private static readonly float[] WallUsSimple = { .34f, .67f, 1f };
        private static readonly float[] ShelfUs = { .34f, .68f, 1f };

        private static float WallShade(float u) => Mathf.Lerp(.93f, 1f, Mathf.Pow(Mathf.Clamp01(u), .6f));

        private static float ShelfShade(float f) => Mathf.Lerp(1f, .95f, Mathf.Clamp01(f));

        private static bool PortalOpen(int side, int tier, int stage)
        {
            if (tier == CaveTier && InSpan(side, MainSide, MainHalf[stage])) return true;
            if (tier == CaveTier + 1 && InSpan(side, MainSide, MainUpperHalf[stage])) return true;
            if (tier == CaveTier && InSpan(side, SecondSide, SecondHalf[stage])) return true;
            if (tier == CaveTier && InSpan(side, TunnelExitSide, MainHalf[stage])) return true;
            if (tier == CaveTier + 1 && InSpan(side, TunnelExitSide, MainUpperHalf[stage])) return true;
            return false;
        }

        private static float BreachWeight(float angle, int breach) =>
            Mathf.Exp(-Mathf.Pow(AngleDelta(angle, BreachAngle[breach]) / BreachWidth[breach], 2f));

        private static float BreachDent(float angle, float nominal)
        {
            float value = 0f;
            for (int breach = 0; breach < BreachAngle.Length; breach++)
            {
                float weight = BreachWeight(angle, breach);
                if (weight < .002f) continue;
                float crest = WallY[BreachTier[breach]];
                float band = Mathf.Clamp01((nominal - BaseY[BreachTier[breach]])
                    / Mathf.Max(1f, crest - BaseY[BreachTier[breach]]));
                float below = Mathf.Clamp01(1f - Mathf.Max(0f, crest - nominal) / 95f);
                float above = Mathf.Clamp01(1f - Mathf.Max(0f, nominal - crest - 60f) / 220f);
                value += BreachDepth[breach] * weight * band * below * above;
            }
            return -value;
        }

        private static float BreachSag(float angle, float nominal)
        {
            float value = 0f;
            for (int breach = 0; breach < BreachAngle.Length; breach++)
            {
                float weight = BreachWeight(angle, breach);
                if (weight < .002f) continue;
                float crest = WallY[BreachTier[breach]];
                value += BreachDrop[breach] * weight * Mathf.Exp(-Mathf.Pow((nominal - crest) / 78f, 2f));
            }
            return -value;
        }

        private static float RadiusAt(int tier, float u) => Mathf.Lerp(TierR[tier], TierR[tier] - Taper[tier], u);

        private static Vector3 WallPoint(int tier, float angle, float u)
        {
            float nominal = Mathf.Lerp(BaseY[tier], WallY[tier], u);
            float height = (nominal + TiltAt(nominal, angle) + BreachSag(angle, nominal)) * HeightScale;
            float radius = (RadiusAt(tier, u) * Shape(angle) * Asym(angle, nominal) + Flute(angle, nominal)
                + Cut(angle, nominal) + BreachDent(angle, nominal) + StepBulge(angle, nominal)) * RadScale;
            radius += Mathf.Sin(angle * 29f + tier * 2.3f) * 1.2f * Mathf.Sin(u * Mathf.PI);
            return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }

        private static Vector3 ShelfPoint(int tier, float angle, float f)
        {
            float nominal = Mathf.Lerp(WallY[tier], TopY[tier], f);
            float height = (nominal + TiltAt(nominal, angle) + BreachSag(angle, nominal)) * HeightScale;
            float share = Shape(angle) * Asym(angle, nominal);
            float outer = (TierR[tier] - Taper[tier]) * share;
            float inner = (tier + 1 < TierCount ? TierR[tier + 1] : SummitR) * share;
            float radius = (Mathf.Lerp(outer, inner, f) + Flute(angle, nominal) + Cut(angle, nominal)
                + BreachDent(angle, nominal)) * RadScale;
            if (f > .02f && f < .98f)
            {
                radius += Mathf.Sin(angle * 17f + tier) * 1.9f * Mathf.Sin(f * Mathf.PI);
                height += Mathf.Sin(angle * 23f + tier * 2f) * .9f * Mathf.Sin(f * Mathf.PI);
            }
            return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }

        private static float SummitRimRadius(float angle) =>
            Mathf.Max(6f, SummitR * Shape(angle) * Asym(angle, SummitY) + Flute(angle, SummitY)
            + Cut(angle, SummitY) + BreachDent(angle, SummitY)) * RadScale;

        private static Vector3 SurfacePoint(float angle, float y)
        {
            for (int tier = 0; tier < TierCount; tier++)
            {
                if (y <= WallY[tier])
                    return WallPoint(tier, angle, Mathf.InverseLerp(BaseY[tier], WallY[tier], y));
                if (y <= TopY[tier])
                    return ShelfPoint(tier, angle, Mathf.InverseLerp(WallY[tier], TopY[tier], y));
            }
            float t = Mathf.Clamp01((y - SummitY) / (PeakHeight - SummitY));
            float radius = SummitRimRadius(angle) * (1f - t);
            float height = (y + TiltAt(y, angle) + BreachSag(angle, y)) * HeightScale;
            return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }

        private static Vector3[] WallRing(int tier, float u)
        {
            var ring = new Vector3[Sides];
            for (int side = 0; side < Sides; side++) ring[side] = WallPoint(tier, SideAngle(side), u);
            return ring;
        }

        private static Vector3[] ShelfRing(int tier, float f)
        {
            var ring = new Vector3[Sides];
            for (int side = 0; side < Sides; side++) ring[side] = ShelfPoint(tier, SideAngle(side), f);
            return ring;
        }

        private static Vector3[] SummitRing(float nominal, float factor)
        {
            var ring = new Vector3[Sides];
            for (int side = 0; side < Sides; side++)
            {
                float angle = SideAngle(side);
                float ragged = .07f * (RingNoise(angle, 3.4f, 5.7f) - .5f)
                    + .04f * (RingNoise(angle, 8.1f, 2.9f) - .5f);
                float r = SummitRimRadius(angle) * factor * (1f + .1f * Mathf.Sin(angle * 5f + 2.3f) + ragged);
                ring[side] = new Vector3(Mathf.Cos(angle) * r,
                    (nominal + TiltAt(nominal, angle) + BreachSag(angle, nominal)) * HeightScale
                    + 3f * (Mathf.Sin(angle * 7f + 1.7f) + 1.6f * (RingNoise(angle, 4.3f, 7.1f) - .5f)),
                    Mathf.Sin(angle) * r);
            }
            return ring;
        }

        public static Mesh Massif()
        {
            var mesh = new CrownRockMesh();
            var rings = new List<Vector3[]>();
            var bands = new List<int>();
            var tones = new List<float>();
            void Add(Vector3[] ring, int code, float tone)
            {
                if (rings.Count > 0) { bands.Add(code); tones.Add(tone); }
                rings.Add(ring);
            }
            Add(WallRing(0, 0f), -1, .93f);
            for (int tier = 0; tier < TierCount; tier++)
            {
                float[] wallUs = tier == CaveTier || tier == CaveTier + 1 ? WallUsDetailed : WallUsSimple;
                float wallPrevious = 0f;
                for (int index = 0; index < wallUs.Length; index++)
                {
                    float u = wallUs[index];
                    Add(WallRing(tier, u), tier * 10 + index, WallShade((wallPrevious + u) * .5f));
                    wallPrevious = u;
                }
                float shelfPrevious = 0f;
                for (int index = 0; index < ShelfUs.Length; index++)
                {
                    float f = ShelfUs[index];
                    Add(ShelfRing(tier, f), -1, ShelfShade((shelfPrevious + f) * .5f));
                    shelfPrevious = f;
                }
            }
            Add(SummitRing(412f, .71f), -1, .95f);
            Add(SummitRing(426f, .43f), -1, .95f);
            for (int ring = 0; ring < rings.Count - 1; ring++)
            {
                Vector3[] lower = rings[ring];
                Vector3[] upper = rings[ring + 1];
                int code = bands[ring];
                int tier = code / 10;
                int stage = code % 10;
                shade = tones[ring];
                for (int side = 0; side < Sides; side++)
                {
                    if (code >= 0 && PortalOpen(side, tier, stage)) continue;
                    int next = (side + 1) % Sides;
                    Tri(mesh, lower[side], upper[side], lower[next]);
                    Tri(mesh, lower[next], upper[side], upper[next]);
                }
            }
            shade = .95f;
            Vector3[] top = rings[rings.Count - 1];
            Vector3 center = Vector3.zero;
            float highest = 0f;
            for (int side = 0; side < Sides; side++)
            {
                center += top[side];
                highest = Mathf.Max(highest, top[side].y);
            }
            center = new Vector3(center.x / Sides, highest + 10f, center.z / Sides);
            for (int side = 0; side < Sides; side++) Tri(mesh, top[side], center, top[(side + 1) % Sides]);
            return mesh.Build("Уступчатый массив");
        }

        public static Mesh Facades()
        {
            var mesh = new CrownRockMesh();
            shade = .98f;
            for (int facade = 0; facade < FacadeAngle.Length; facade++) Facade(mesh, facade);
            return mesh.Build("Скальные фасады");
        }

        private static void Facade(CrownRockMesh mesh, int facade)
        {
            float half = FacadeHalf[facade];
            float depth = FacadeDepth[facade];
            var rows = new List<Vector3[]>();
            for (int tier = FacadeFirst[facade]; tier <= FacadeLast[facade]; tier++)
            {
                float[] levels = { 0f, .45f, .8f, 1f };
                foreach (float u in levels)
                {
                    var row = new Vector3[Mathf.CeilToInt(half) * 2 + 1];
                    for (int cell = 0; cell < row.Length; cell++)
                    {
                        float offset = cell - (row.Length - 1) * .5f;
                        float angle = FacadeAngle[facade] + offset * Tau / Sides;
                        float nominal = Mathf.Lerp(BaseY[tier], WallY[tier], u);
                        Vector3 surface = SurfacePoint(angle, nominal);
                        float falloff = Mathf.Cos(Mathf.Clamp01(Mathf.Abs(offset) / (half + .5f)) * Mathf.PI * .5f);
                        float weathering = Mathf.PerlinNoise(cell * .7f + facade * 7.3f, tier * 1.9f) - .5f;
                        float grit = Mathf.PerlinNoise(cell * 2.3f + facade * 3.1f, tier * 3.7f + 4.1f) - .5f;
                        float notch = Mathf.Pow(Mathf.Max(0f, Mathf.PerlinNoise(cell * 1.1f + facade * 5.9f,
                            tier * 2.3f + 1.3f) - .62f) / .38f, 2f);
                        float reach = depth * falloff * (1f - .3f * (nominal - BaseY[tier]) / 40f)
                            * (1f + .3f * weathering + .12f * grit - .45f * notch);
                        float radius = FlatRadius(surface) + Mathf.Max(2f, reach);
                        row[cell] = new Vector3(Mathf.Cos(angle) * radius,
                            surface.y + .5f + 1.6f * (grit + .5f * weathering),
                            Mathf.Sin(angle) * radius);
                    }
                    rows.Add(row);
                }
            }
            for (int row = 0; row < rows.Count - 1; row++)
                for (int cell = 0; cell < rows[row].Length - 1; cell++)
                    Panel(mesh, rows[row][cell], rows[row][cell + 1], rows[row + 1][cell + 1], rows[row + 1][cell]);
        }

        public static Mesh Ribs()
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            float[] dominant = { FacadeAngle[0] + .62f, FacadeAngle[0] + 1.5f, FacadeAngle[1] + .7f, FacadeAngle[2] + 1.35f };
            for (int rib = 0; rib < dominant.Length; rib++)
                Fin(mesh, dominant[rib], 0, rib % 2 == 0 ? TierCount - 2 : TierCount - 1, rib * 41 + 7, true);
            for (int rib = 0; rib < 12; rib++)
            {
                float angle = (rib + .5f) * Tau / 24f + 1.7f;
                if (FrontDistance(angle) < 34f || NearFacade(angle, .35f)) continue;
                Fin(mesh, angle, 1, TierCount - 1, rib * 13 + 5, false);
            }
            for (int rib = 0; rib < 24; rib++)
            {
                float angle = (rib + .5f) * Tau / 24f;
                if (FrontDistance(angle) < 26f || NearFacade(angle, .12f)) continue;
                Fin(mesh, angle, rib % 4, Mathf.Min(TierCount - 1, rib % 4 + 2 + rib % 2), rib * 29 + 61, false);
            }
            Fin(mesh, -Mathf.PI * .5f - .78f, 3, TierCount - 1, 311, true);
            Fin(mesh, -Mathf.PI * .5f + .78f, 3, TierCount - 1, 337, true);
            return mesh.Build("Скальные рёбра");
        }

        private static bool NearFacade(float angle, float margin)
        {
            for (int facade = 0; facade < FacadeAngle.Length; facade++)
                if (Mathf.Abs(AngleDelta(angle, FacadeAngle[facade])) < (FacadeHalf[facade] * .5f + margin) * Tau / Sides)
                    return true;
            return false;
        }

        private static void Fin(CrownRockMesh mesh, float angle, int first, int last, int seed, bool dominant)
        {
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            float half = (1.1f + 1.15f * (.5f + .5f * Mathf.Sin(seed * 2.7f))) * Tau / Sides;
            float strength = dominant
                ? 22f + 16f * (.5f + .5f * Mathf.Sin(seed * 1.7f))
                : 11f + 13f * (.5f + .5f * Mathf.Sin(seed * 1.7f));
            var heights = new List<float>();
            for (int tier = first; tier <= last; tier++)
            {
                heights.Add(BaseY[tier] + 2f);
                heights.Add(WallY[tier]);
                heights.Add(TopY[tier]);
            }
            var ridge = new List<Vector3>();
            var left = new List<Vector3>();
            var right = new List<Vector3>();
            for (int index = 0; index < heights.Count; index++)
            {
                float nominal = heights[index];
                float progress = Mathf.InverseLerp(BaseY[first], TopY[last], nominal);
                float weather = Mathf.PerlinNoise(nominal * .037f + seed * 1.7f, seed * .9f) - .5f;
                float grit = Mathf.PerlinNoise(nominal * .16f + seed * 3.1f, seed * 1.3f + 4.7f) - .5f;
                float protrusion = strength * (1f - progress * .5f) * (.86f + .5f * weather + .2f * grit);
                Vector3 surface = SurfacePoint(angle, nominal);
                float radius = FlatRadius(surface) + protrusion;
                float height = surface.y + .9f + 1.4f * grit;
                ridge.Add(new Vector3(direction.x * radius, height, direction.z * radius)
                    + tangent * (2.4f * (Mathf.PerlinNoise(nominal * .03f + seed, seed * 2.3f) - .5f)
                        + .9f * (Mathf.PerlinNoise(nominal * .11f + seed * 2.7f, seed) - .5f)));
                Vector3 leftSurface = SurfacePoint(angle - half, nominal);
                Vector3 rightSurface = SurfacePoint(angle + half, nominal);
                left.Add(new Vector3(leftSurface.x, height, leftSurface.z) + direction * .8f);
                right.Add(new Vector3(rightSurface.x, height, rightSurface.z) + direction * .8f);
            }
            for (int index = 0; index < heights.Count - 1; index++)
            {
                Panel(mesh, left[index], ridge[index], ridge[index + 1], left[index + 1]);
                Panel(mesh, ridge[index], right[index], right[index + 1], ridge[index + 1]);
            }
            int lastIndex = heights.Count - 1;
            float width = Mathf.Max(9f, half * FlatRadius(ridge[lastIndex]) * .7f
                * (.7f + .75f * Mathf.PerlinNoise(seed * 1.9f, seed * .3f)));
            Vector3 crownLeft = ridge[lastIndex] - tangent * width * (.85f + .3f * Mathf.PerlinNoise(seed * 2.3f, 2.1f))
                + Vector3.up * (3.4f * (Mathf.PerlinNoise(seed * 3.1f, 5.7f) - .5f));
            Vector3 crownRight = ridge[lastIndex] + tangent * width * (.85f + .3f * Mathf.PerlinNoise(seed * 4.1f, 7.3f))
                + Vector3.up * (3.4f * (Mathf.PerlinNoise(seed * 5.3f, 1.9f) - .5f));
            Panel(mesh, left[lastIndex], crownLeft, crownRight, right[lastIndex]);
            Twin(mesh, ridge[0], right[0], left[0]);
        }

        public static Mesh Ledges()
        {
            var mesh = new CrownRockMesh();
            shade = .85f;
            var random = new System.Random(5150);
            for (int tier = 0; tier < TierCount; tier++)
                for (int ledge = 0; ledge < 6; ledge++)
                {
                    if (tier >= 6) continue;
                    float angle = (float)random.NextDouble() * Tau;
                    if (tier >= 3 && FrontDistance(angle) < 34f) continue;
                    float nominal = Mathf.Lerp(BaseY[tier] + 10f, WallY[tier] - 9f, (float)random.NextDouble());
                    float y = nominal + TiltAt(nominal, angle);
                    float half = (.5f + (float)random.NextDouble() * 1.2f) * Tau / Sides;
                    float depth = 2f + (float)random.NextDouble() * 4f;
                    float thickness = 2f + (float)random.NextDouble() * 2.4f;
                    int segments = 3 + random.Next(2);
                    float ledgeSeed = tier * 13.7f + ledge * 5.3f;
                    var innerTop = new Vector3[segments + 1];
                    var innerBottom = new Vector3[segments + 1];
                    var outerTop = new Vector3[segments + 1];
                    var outerBottom = new Vector3[segments + 1];
                    for (int edge = 0; edge <= segments; edge++)
                    {
                        float t = edge / (float)segments - .5f;
                        float bite = Ragged(angle + t * half * 2f, ledgeSeed + edge * 3.1f, .9f);
                        float edgeAngle = angle + t * half * 2f * (1f + .42f * bite);
                        Vector3 wallLow = SurfacePoint(edgeAngle, nominal);
                        Vector3 wallHigh = SurfacePoint(edgeAngle, nominal + thickness);
                        float reach = depth * Mathf.Lerp(1f, .55f, Mathf.Abs(t) * 2f) * Mathf.Max(.3f, 1f + 1.1f * bite);
                        float outerRadius = FlatRadius(wallLow) + reach;
                        Vector3 buried = new Vector3(-Mathf.Cos(edgeAngle), 0f, -Mathf.Sin(edgeAngle)) * 2.2f;
                        innerTop[edge] = wallHigh + buried;
                        innerBottom[edge] = wallLow + buried;
                        outerTop[edge] = Polar(edgeAngle, outerRadius, wallLow.y + thickness
                            * (.55f + .5f * Mathf.Clamp01(Ragged(edgeAngle, ledgeSeed + 31f, 1.1f) + .5f)));
                        outerBottom[edge] = Polar(edgeAngle, outerRadius + .4f, wallLow.y - thickness
                            * (.25f + .55f * Mathf.Clamp01(Ragged(edgeAngle, ledgeSeed + 47f, 1f) + .5f)));
                    }
                    for (int edge = 0; edge < segments; edge++)
                    {
                        Panel(mesh, innerTop[edge], innerTop[edge + 1], outerTop[edge + 1], outerTop[edge]);
                        shade = .96f;
                        Panel(mesh, outerTop[edge], outerTop[edge + 1], outerBottom[edge + 1], outerBottom[edge]);
                        shade = .92f;
                        Panel(mesh, outerBottom[edge], outerBottom[edge + 1], innerBottom[edge + 1], innerBottom[edge]);
                    }
                    shade = .88f;
                }
            var jumpRandom = new System.Random(606);
            for (int step = 0; step < JumpSteps.Length; step++)
            {
                JumpStep jump = JumpSteps[step];
                float angleRad = jump.Angle * Mathf.Deg2Rad;
                Vector3 surface = SurfacePoint(angleRad, jump.Y);
                Vector3 outward = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
                float scale = 2.9f + (float)jumpRandom.NextDouble() * .5f;
                FlatStone(mesh, surface + outward * (scale * .55f), scale * .5f, surface.y + .12f, step * 53 + 9);
            }
            return mesh.Build("Скальные карнизы");
        }

        public static Mesh Spires()
        {
            var mesh = new CrownRockMesh();
            shade = .95f;
            var random = new System.Random(7211);
            for (int tooth = 0; tooth < 22; tooth++)
            {
                if (random.NextDouble() < .14) continue;
                float angle = tooth * Tau / 22f + ((float)random.NextDouble() - .5f) * .22f;
                float nominal = Mathf.Lerp(356f, 396f, (float)random.NextDouble());
                float surface = FlatRadius(SurfacePoint(angle, nominal));
                float radius = surface * Mathf.Lerp(.12f, .64f, (float)random.NextDouble());
                float power = Mathf.Pow((float)random.NextDouble(), 2.1f);
                float height = Mathf.Lerp(14f, 58f, power) * 1.4f;
                float width = Mathf.Lerp(16f, 42f, power) * 1.4f;
                float lean = random.NextDouble() < .14 ? ((float)random.NextDouble() - .5f) * height * .5f : 0f;
                Tooth(mesh, angle, radius, nominal, height, width, tooth * 1.7f, lean);
            }
            for (int tooth = 0; tooth < 5; tooth++)
            {
                float angle = 2.1f + tooth * 1.05f;
                float nominal = Mathf.Lerp(372f, 400f, (float)random.NextDouble());
                float surface = FlatRadius(SurfacePoint(angle, nominal));
                Tooth(mesh, angle, surface * Mathf.Lerp(.18f, .5f, (float)random.NextDouble()), nominal,
                    Mathf.Lerp(20f, 44f, (float)random.NextDouble()) * 1.4f,
                    Mathf.Lerp(18f, 34f, (float)random.NextDouble()) * 1.4f, tooth * 2.3f + 1.1f, 0f);
            }
            Tooth(mesh, 2.1f, 34f, 386f, 104f, 62f, 4.3f, 6f);
            Tooth(mesh, 5.4f, 40f, 380f, 92f, 66f, 6.9f, -5f);
            Tooth(mesh, 3.9f, 26f, 392f, 72f, 48f, 9.1f, 3f);
            Tooth(mesh, 1.2f, 22f, 388f, 66f, 44f, 11.3f, -8f);
            return mesh.Build("Скальный гребень вершины");
        }

        private static void Tooth(CrownRockMesh mesh, float angle, float radius, float nominal, float height, float width,
            float seed, float lean)
        {
            const int sides = 11;
            float[] levels = { 0f, .22f, .44f, .68f, .86f, 1f };
            float[] scales = { 1f, .98f, .9f, .76f, .58f, .4f };
            float baseY = nominal + TiltAt(nominal, angle);
            Vector3 center = Polar(angle, radius, 0f);
            Vector3 tilt = Polar(angle + Mathf.PI * .5f, lean, 0f);
            Vector3[] previous = null;
            for (int level = 0; level < levels.Length; level++)
            {
                float t = levels[level];
                var ring = new Vector3[sides];
                for (int side = 0; side < sides; side++)
                {
                    float sideAngle = side * Tau / sides + seed
                        + .16f * (Hash(side * 3.7f + seed) - .5f) * level;
                    float weather = Mathf.PerlinNoise(side * 1.9f + seed * 2.3f, level * 1.7f + seed) - .5f;
                    float grit = Hash(side * 5.3f + level * 11.7f + seed * 3.1f) - .5f;
                    float notch = Mathf.Pow(Mathf.Max(0f, Hash(side * 2.9f + level * 7.1f + seed) - .68f) / .32f, 2f);
                    float r = width * scales[level] * (1f + .3f * weather + .16f * grit - .5f * notch);
                    ring[side] = center + tilt * t + new Vector3(Mathf.Cos(sideAngle) * r,
                        baseY + height * t * (.94f + .13f * (Hash(side * 1.3f + seed * 1.7f) - .5f)),
                        Mathf.Sin(sideAngle) * r);
                }
                if (previous != null)
                    for (int side = 0; side < sides; side++)
                        Panel(mesh, previous[side], ring[side], ring[(side + 1) % sides], previous[(side + 1) % sides]);
                previous = ring;
            }
            Vector3 crown = center + tilt + new Vector3(
                (Hash(seed * 2.7f + 1.1f) - .5f) * width * .7f,
                baseY + height * (.96f + .1f * Hash(seed * 3.9f + 5.3f)),
                (Hash(seed * 4.7f + 7.7f) - .5f) * width * .7f);
            for (int side = 0; side < sides; side++)
                Twin(mesh, previous[side], crown, previous[(side + 1) % sides]);
        }

        public static Mesh Balcony() => BalconyAt(MainSide, 58f, 5150f, "Площадка перед гротом");

        public static Mesh ExitBalcony() => BalconyAt(TunnelExitSide, 46f, 7150f, "Площадка за гротом");

        private static Mesh BalconyAt(int side, float reach, float seed, string name)
        {
            var mesh = new CrownRockMesh();
            shade = .9f;
            const int spread = 6;
            int start = side - spread;
            int end = side + spread;
            var inner = new Vector3[end - start + 1];
            var middle = new Vector3[end - start + 1];
            var outer = new Vector3[end - start + 1];
            var cliff = new Vector3[end - start + 1];
            for (int index = 0; index < inner.Length; index++)
            {
                int cellSide = start + index;
                float angle = SideAngle(cellSide);
                Vector3 edge = WallPoint(CaveTier, angle, 0f);
                float innerRadius = FlatRadius(edge);
                float outerRadius = innerRadius + reach * Mathf.Max(.4f, 1f + .55f * Ragged(angle, seed, 1.6f))
                    + 5f * Ragged(angle, seed + 613f, .9f);
                inner[index] = edge - new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.2f;
                middle[index] = Polar(angle, Mathf.Lerp(innerRadius, outerRadius, .5f),
                    edge.y - 1.4f + 1.6f * Ragged(angle, seed + 811f, .8f));
                outer[index] = Polar(angle, outerRadius, edge.y - 2.2f + 3.2f * Ragged(angle, seed + 919f, 1.2f));
                cliff[index] = Polar(angle, outerRadius + 3f * Mathf.Max(0f, 1f + Ragged(angle, seed + 1013f, 1f)),
                    edge.y - 21f + 7f * Ragged(angle, seed + 1129f, 1.3f));
            }
            for (int index = 0; index < inner.Length - 1; index++)
            {
                Panel(mesh, inner[index], middle[index], middle[index + 1], inner[index + 1]);
                Panel(mesh, middle[index], outer[index], outer[index + 1], middle[index + 1]);
                Panel(mesh, outer[index], cliff[index], cliff[index + 1], outer[index + 1]);
                shade = .86f;
            }
            return mesh.Build(name);
        }

        public static Mesh GrottoInterior()
        {
            var mesh = new CrownRockMesh();
            shade = .56f;
            Portal(mesh, SecondSide, 2, 62f, .45f, 0, 4);
            shade = 1f;
            return mesh.Build("Боковая ниша грота");
        }

        private static readonly int[] PortalRowTier = { 4, 4, 4, 4, 4, 4, 5, 5, 5 };
        private static readonly float[] PortalRowU = { 0f, .2f, .4f, .6f, .78f, 1f, .2f, .4f, .6f };

        private static void Portal(CrownRockMesh mesh, int side, int half, float depth, float pinch, int rowFrom, int rowTo)
        {
            int count = half * 2 + 1;
            int rows = rowTo - rowFrom + 1;
            var rim = new Vector3[rows, count];
            var back = new Vector3[rows, count];
            for (int row = 0; row < rows; row++)
            {
                int source = rowFrom + row;
                for (int index = 0; index < count; index++)
                {
                    float angle = SideAngle(side - half + index);
                    rim[row, index] = WallPoint(PortalRowTier[source], angle, PortalRowU[source]);
                    back[row, index] = Pinch(Inward(rim[row, index], depth), SideAngle(side), pinch);
                }
            }
            for (int row = 0; row < rows - 1; row++)
            {
                float inner = Mathf.Lerp(.5f, .3f, row / (float)Mathf.Max(1, rows - 1));
                shade = inner;
                for (int index = 0; index < count - 1; index++)
                    Panel(mesh, back[row, index], back[row + 1, index], back[row + 1, index + 1], back[row, index + 1]);
            }
            shade = .58f;
            for (int index = 0; index < count - 1; index++)
            {
                Panel(mesh, rim[0, index], rim[0, index + 1], back[0, index + 1], back[0, index]);
                Panel(mesh, rim[rows - 1, index], back[rows - 1, index], back[rows - 1, index + 1], rim[rows - 1, index + 1]);
            }
            for (int row = 0; row < rows - 1; row++)
            {
                shade = Mathf.Lerp(.52f, .26f, row / (float)Mathf.Max(1, rows - 1));
                Panel(mesh, rim[row, 0], rim[row + 1, 0], back[row + 1, 0], back[row, 0]);
                Panel(mesh, rim[row, count - 1], back[row, count - 1], back[row + 1, count - 1], rim[row + 1, count - 1]);
            }
        }

        public static Mesh GrottoBrow()
        {
            var mesh = new CrownRockMesh();
            shade = .92f;
            Brow(mesh, MainSide, 15, 18f, 64f, 9f, 8f, 62f, CaveTier + 1, CaveCeilingU);
            Brow(mesh, SecondSide, 10, 9f, 24f, 6f, 6f, 28f, CaveTier, .78f);
            float mouthY = WallPoint(CaveTier, SideAngle(MainSide), 0f).y;
            float mouthRadius = FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f));
            float ceilingY = CaveCeilingHeight(0f);
            shade = .86f;
            Chunk(mesh, Polar(SideAngle(MainSide - 4), mouthRadius + 13f, mouthY + 30f),
                new Vector3(20f, 52f, 20f), 11);
            Chunk(mesh, Polar(SideAngle(MainSide + 4), mouthRadius + 13f, mouthY + 30f),
                new Vector3(20f, 50f, 20f), 23);
            Chunk(mesh, Polar(SideAngle(MainSide - 4), mouthRadius + 16f, mouthY + 62f),
                new Vector3(14f, 22f, 14f), 37);
            Chunk(mesh, Polar(SideAngle(MainSide + 4), mouthRadius + 16f, mouthY + 62f),
                new Vector3(13f, 21f, 13f), 41);
            Chunk(mesh, Polar(SideAngle(MainSide), mouthRadius + 24f, ceilingY + 14f),
                new Vector3(20f, 16f, 16f), 53);
            Chunk(mesh, Polar(SideAngle(MainSide - 5), mouthRadius + 14f, mouthY + 10f),
                new Vector3(13f, 20f, 13f), 67);
            Chunk(mesh, Polar(SideAngle(MainSide + 5), mouthRadius + 14f, mouthY + 10f),
                new Vector3(13f, 20f, 13f), 71);
            shade = 1f;
            return mesh.Build("Скальная обвязка грота");
        }

        public static Mesh ExitGrottoBrow()
        {
            var mesh = new CrownRockMesh();
            shade = .92f;
            Brow(mesh, TunnelExitSide, 13, 14f, 48f, 8f, 7f, 50f, CaveTier + 1, CaveCeilingU);
            float mouthY = WallPoint(CaveTier, SideAngle(TunnelExitSide), 0f).y;
            float mouthRadius = FlatRadius(WallPoint(CaveTier, SideAngle(TunnelExitSide), 0f));
            shade = .86f;
            Chunk(mesh, Polar(SideAngle(TunnelExitSide - 3), mouthRadius + 11f, mouthY + 24f),
                new Vector3(17f, 40f, 17f), 711);
            Chunk(mesh, Polar(SideAngle(TunnelExitSide + 3), mouthRadius + 11f, mouthY + 24f),
                new Vector3(16f, 38f, 16f), 723);
            Chunk(mesh, Polar(SideAngle(TunnelExitSide), mouthRadius + 19f, mouthY + 48f),
                new Vector3(14f, 20f, 14f), 737);
            shade = 1f;
            return mesh.Build("Скальная обвязка выхода грота");
        }

        private static readonly float[] TunnelShafts = { .16f, .48f, .8f };
        private static Vector3[] tunnelPath;

        public static Vector3[] TunnelCenterline()
        {
            if (tunnelPath != null) return tunnelPath;
            float mouth = FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f));
            float exitMouth = FlatRadius(WallPoint(CaveTier, SideAngle(TunnelExitSide), 0f));
            float start = CaveFloorHeight(0f);
            float end = CaveFloorHeight(-180f);
            tunnelPath = new[]
            {
                new Vector3(0f, start, -(mouth - 1f)),
                new Vector3(-16f, start + 2.5f, -(mouth - 34f)),
                new Vector3(-22f, start + 4.5f, -(mouth * .62f)),
                new Vector3(18f, start + 5.5f, -(mouth * .26f)),
                new Vector3(-13f, start + 5f, mouth * .06f),
                new Vector3(20f, start + 3.5f, exitMouth * .34f),
                new Vector3(-12f, end + 2f, exitMouth * .68f),
                new Vector3(0f, end, exitMouth - 1f)
            };
            return tunnelPath;
        }

        private static Vector3 SampleTunnel(float t)
        {
            var path = TunnelCenterline();
            float total = 0f;
            for (int index = 1; index < path.Length; index++) total += Vector3.Distance(path[index - 1], path[index]);
            float target = Mathf.Clamp01(t) * total;
            float walked = 0f;
            for (int index = 1; index < path.Length; index++)
            {
                float segment = Vector3.Distance(path[index - 1], path[index]);
                if (walked + segment >= target || index == path.Length - 1)
                    return Vector3.Lerp(path[index - 1], path[index], Mathf.Clamp01((target - walked) / Mathf.Max(.001f, segment)));
                walked += segment;
            }
            return path[path.Length - 1];
        }

        public static bool TryTunnelFloor(Vector3 point, out float floor)
        {
            var path = TunnelCenterline();
            float best = float.MaxValue;
            float height = 0f;
            var probe = new Vector2(point.x, point.z);
            for (int index = 1; index < path.Length; index++)
            {
                Vector3 first = path[index - 1], second = path[index];
                var start = new Vector2(first.x, first.z);
                var span = new Vector2(second.x - first.x, second.z - first.z);
                float t = Mathf.Clamp01(Vector2.Dot(probe - start, span) / Mathf.Max(.001f, span.sqrMagnitude));
                float distance = (probe - (start + span * t)).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    height = Mathf.Lerp(first.y, second.y, t);
                }
            }
            floor = height;
            return best <= 144f && point.y < height + 18f && point.y > height - 8f;
        }

        public static Mesh GrottoTunnel()
        {
            var mesh = new CrownRockMesh();
            var path = TunnelCenterline();
            var centers = new List<Vector3>();
            for (int index = 1; index < path.Length; index++)
            {
                Vector3 first = path[index - 1], second = path[index];
                int count = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(first, second) / 6f));
                for (int step = 0; step < count; step++) centers.Add(Vector3.Lerp(first, second, step / (float)count));
            }
            centers.Add(path[path.Length - 1]);
            int rings = centers.Count;
            var sides = new Vector3[rings];
            for (int index = 0; index < rings; index++)
            {
                Vector3 back = centers[Mathf.Max(0, index - 1)];
                Vector3 ahead = centers[Mathf.Min(rings - 1, index + 1)];
                Vector3 tangent = ahead - back;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < .001f) tangent = Vector3.forward;
                sides[index] = Vector3.Cross(Vector3.up, tangent.normalized);
            }
            int[] holes = new int[TunnelShafts.Length];
            for (int shaft = 0; shaft < TunnelShafts.Length; shaft++)
            {
                Vector3 target = SampleTunnel(TunnelShafts[shaft]);
                int nearest = 0;
                float closest = float.MaxValue;
                for (int index = 0; index < rings; index++)
                {
                    float distance = (centers[index] - target).sqrMagnitude;
                    if (distance < closest) { closest = distance; nearest = index; }
                }
                holes[shaft] = Mathf.Clamp(nearest, 0, rings - 2);
            }
            float[] lateral = { -1f, -.95f, -.62f, 0f, .62f, .95f, 1f };
            float[] vertical = { 0f, .55f, 1f, 1.08f, 1f, .55f, 0f };
            var ringPoints = new Vector3[rings, lateral.Length];
            for (int index = 0; index < rings; index++)
            {
                Vector3 basePoint = centers[index];
                float half = 10.5f + 1.1f * Mathf.Sin(index * .63f + 1.2f);
                float height = 15f + 1.4f * Mathf.Sin(index * .47f - .4f);
                for (int edge = 0; edge < lateral.Length; edge++)
                    ringPoints[index, edge] = basePoint + sides[index] * (lateral[edge] * half)
                        + Vector3.up * (vertical[edge] * height);
            }
            for (int index = 0; index < rings - 1; index++)
            {
                bool hole = false;
                for (int shaft = 0; shaft < holes.Length; shaft++)
                    if (index == holes[shaft] || index == Mathf.Min(rings - 2, holes[shaft] + 1)) hole = true;
                shade = Mathf.Lerp(.6f, .68f, Mathf.PerlinNoise(index * .31f, 1.7f));
                Panel(mesh, ringPoints[index, 6], ringPoints[index, 0], ringPoints[index + 1, 0], ringPoints[index + 1, 6]);
                for (int edge = 0; edge < lateral.Length - 1; edge++)
                {
                    if (hole && edge >= 2 && edge <= 3) continue;
                    shade = Mathf.Lerp(.52f, .82f, edge / (float)(lateral.Length - 2));
                    Panel(mesh, ringPoints[index, edge], ringPoints[index + 1, edge],
                        ringPoints[index + 1, edge + 1], ringPoints[index, edge + 1]);
                }
            }
            shade = .5f;
            TunnelScreen(mesh, SideAngle(MainSide), FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f)) - 2f,
                CaveFloorHeight(0f), 58f, 116f, 31);
            TunnelScreen(mesh, SideAngle(TunnelExitSide),
                FlatRadius(WallPoint(CaveTier, SideAngle(TunnelExitSide), 0f)) - 2f,
                CaveFloorHeight(-180f), 58f, 116f, 47);
            return mesh.Build("Сквозной грот — туннель");
        }

        private static void TunnelScreen(CrownRockMesh mesh, float angle, float radius, float floorY, float spread,
            float height, int seed)
        {
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 lateral = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 center = outward * radius + Vector3.up * floorY;
            Vector3 At(float along, float up) => center + lateral * along + Vector3.up * up;
            const int columns = 32;
            const float mouth = 10.6f;
            var rim = new Vector3[columns + 1];
            var along = new float[columns + 1];
            for (int index = 0; index <= columns; index++)
            {
                float x = Mathf.Lerp(-spread, spread, index / (float)columns);
                along[index] = x;
                float half = mouth * (1f + .18f * (Mathf.PerlinNoise(index * .83f + seed * .11f, seed * .73f) - .5f));
                float arch = Mathf.Abs(x) >= half ? -7f
                    : 16f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(x / half, 2f)));
                if (arch > -7f)
                {
                    arch *= .88f + .26f * Mathf.PerlinNoise(index * 1.7f + seed * .37f, seed * .7f + 3.1f);
                    arch += (Mathf.PerlinNoise(index * 2.9f + seed * .53f, 1.3f + seed * .13f) - .5f) * 2.4f;
                }
                rim[index] = At(x, Mathf.Max(-7f, arch));
            }
            for (int index = 0; index < columns; index++)
            {
                float middle = (index + .5f) / columns * 2f - 1f;
                shade = Mathf.Lerp(.94f, .6f, Mathf.Abs(middle));
                Panel(mesh, rim[index], rim[index + 1], At(along[index + 1], height), At(along[index], height));
            }
            for (int index = 4; index < columns - 4; index += 7)
            {
                float lip = Mathf.Max(-7f, rim[index].y - center.y);
                if (lip <= 1f) continue;
                Chunk(mesh, rim[index] + Vector3.up * 3.2f + outward * 1.2f,
                    new Vector3(3.6f, 4.8f, 3.6f), seed * 7 + index * 13);
            }
        }

        public static Mesh GrottoShafts()
        {
            var mesh = new CrownRockMesh();
            for (int shaft = 0; shaft < TunnelShafts.Length; shaft++)
            {
                Vector3 center = SampleTunnel(TunnelShafts[shaft]);
                float top = OuterSurfaceHeight(center);
                float bottom = center.y + 17f;
                if (top < bottom + 12f) continue;
                const int sides = 9;
                var lower = new Vector3[sides];
                var upper = new Vector3[sides];
                float lowerRadius = 6.2f;
                float upperRadius = 4.6f + 1.6f * (shaft % 2);
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Tau / sides;
                    lower[side] = center + new Vector3(Mathf.Cos(angle) * lowerRadius, 0f, Mathf.Sin(angle) * lowerRadius)
                        + Vector3.up * (bottom - center.y);
                    float rim = .86f + .28f * Hash(side * 3.1f + shaft * 7.7f);
                    upper[side] = center + new Vector3(Mathf.Cos(angle) * upperRadius * rim, 0f,
                        Mathf.Sin(angle) * upperRadius * rim) + Vector3.up * (top - center.y - 1.6f);
                }
                shade = .6f;
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Panel(mesh, lower[side], upper[side], upper[next], lower[next]);
                }
                shade = .88f;
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Vector3 outerA = upper[side] + new Vector3(upper[side].x - center.x, 0f, upper[side].z - center.z).normalized * 2.4f;
                    Vector3 outerB = upper[next] + new Vector3(upper[next].x - center.x, 0f, upper[next].z - center.z).normalized * 2.4f;
                    Panel(mesh, upper[side], outerA, outerB, upper[next]);
                }
            }
            return mesh.Build("Световые шахты грота");
        }

        private static void Brow(CrownRockMesh mesh, int side, int spread, float minReach, float maxReach,
            float drop, float lift, float back, int tier, float u)
        {
            float crownNominal = Mathf.Lerp(BaseY[tier], WallY[tier], u)
                + TiltAt(Mathf.Lerp(BaseY[tier], WallY[tier], u), SideAngle(side));
            for (int cell = side - spread; cell < side + spread; cell++)
            {
                float first = SideAngle(cell);
                float second = SideAngle(cell + 1);
                float center = 1f - Mathf.Abs(cell + .5f - side) / (spread + .5f);
                float reach = minReach + (maxReach - minReach) * Mathf.Max(0f, center);
                float reachFirst = reach * Mathf.Max(.45f, 1f + .5f * Ragged(first, side, 1.1f));
                float reachSecond = reach * Mathf.Max(.45f, 1f + .5f * Ragged(second, side, 1.1f));
                Vector3 innerA = WallPoint(tier, first, u);
                Vector3 innerB = WallPoint(tier, second, u);
                float radiusA = FlatRadius(innerA) + .4f;
                float radiusB = FlatRadius(innerB) + .4f;
                innerA -= new Vector3(Mathf.Cos(first), 0f, Mathf.Sin(first)) * 1.8f;
                innerB -= new Vector3(Mathf.Cos(second), 0f, Mathf.Sin(second)) * 1.8f;
                Vector3 lipA = Polar(first, radiusA + reachFirst,
                    innerA.y - drop + 4.4f * Ragged(first, side + 7.3f, 1.5f));
                Vector3 lipB = Polar(second, radiusB + reachSecond,
                    innerB.y - drop + 4.4f * Ragged(second, side + 7.3f, 1.5f));
                Vector3 faceA = Polar(first, radiusA + reachFirst * .92f,
                    innerA.y + lift + 3.8f * Ragged(first, side + 2.9f, 1.2f));
                Vector3 faceB = Polar(second, radiusB + reachSecond * .92f,
                    innerB.y + lift + 3.8f * Ragged(second, side + 2.9f, 1.2f));
                Vector3 crownA = Polar(first, radiusA - back,
                    crownNominal + 20f + 4.2f * Ragged(first, side + 5.1f, 1f));
                Vector3 crownB = Polar(second, radiusB - back,
                    crownNominal + 20f + 4.2f * Ragged(second, side + 5.1f, 1f));
                shade = .88f;
                Panel(mesh, innerA, lipA, lipB, innerB);
                shade = .82f;
                Panel(mesh, lipA, faceA, faceB, lipB);
                shade = .94f;
                Panel(mesh, faceA, crownA, crownB, faceB);
                shade = 1f;
            }
        }

        public static Mesh ShelfGrass()
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            var random = new System.Random(3311);
            float[] steps = { 0f, .34f, .68f, 1f };
            for (int tier = 0; tier < TierCount; tier++)
            {
                int patches = tier == TierCount - 1 ? 5 : WideShelf[tier] > 0 ? 12 : 6;
                for (int patch = 0; patch < patches; patch++)
                {
                    int side = random.Next(Sides);
                    int width = 1 + random.Next(WideShelf[tier] > 0 ? 4 : 2);
                    int band = random.Next(3);
                    bool clear = (tier == 3 && FrontDistance(SideAngle(side)) < 36f) || (tier >= 4 && FrontDistance(SideAngle(side)) < 32f);
                    if (clear) continue;
                    for (int step = 0; step < width; step++)
                    {
                        Vector3 a = ShelfPoint(tier, SideAngle(side + step), steps[band]) + Vector3.up * .6f;
                        Vector3 b = ShelfPoint(tier, SideAngle(side + step + 1), steps[band]) + Vector3.up * .6f;
                        Vector3 c = ShelfPoint(tier, SideAngle(side + step), steps[band + 1]) + Vector3.up * .6f;
                        Vector3 d = ShelfPoint(tier, SideAngle(side + step + 1), steps[band + 1]) + Vector3.up * .6f;
                        Tri(mesh, a, c, b);
                        Tri(mesh, c, d, b);
                    }
                }
            }
            for (int patch = 0; patch < 6; patch++)
            {
                float angle = -Mathf.PI * .5f + (patch - 2.5f) * .085f;
                int side = Mathf.RoundToInt(angle / Tau * Sides);
                int band = patch % 3;
                Vector3 a = ShelfPoint(3, SideAngle(side), steps[band]) + Vector3.up * .6f;
                Vector3 b = ShelfPoint(3, SideAngle(side + 1), steps[band]) + Vector3.up * .6f;
                Vector3 c = ShelfPoint(3, SideAngle(side), steps[band + 1]) + Vector3.up * .6f;
                Vector3 d = ShelfPoint(3, SideAngle(side + 1), steps[band + 1]) + Vector3.up * .6f;
                Tri(mesh, a, c, b);
                Tri(mesh, c, d, b);
            }
            return mesh.Build("Травяные полки");
        }

        public static Mesh MossFringe()
        {
            var mesh = new CrownRockMesh();
            shade = .9f;
            for (int tier = 0; tier < TierCount; tier++)
            {
                if (WideShelf[tier] == 0) continue;
                for (int step = 0; step < 22; step++)
                {
                    float angle = step / 22f * Tau + (tier % 3) * .017f + Mathf.Sin(step * 7.1f + tier) * .01f;
                    if (FrontDistance(angle) < 32f) continue;
                    float drop = 1.8f + 2.4f * (.5f + .5f * Mathf.Sin(step * 3.3f + tier * 1.9f));
                    float width = .9f + 1.3f * (.5f + .5f * Mathf.Sin(step * 5.7f + tier));
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    Vector3 topCenter = WallPoint(tier, angle, 1f) + direction * .7f + Vector3.up * .5f;
                    Vector3 topA = topCenter + tangent * width;
                    Vector3 topB = topCenter - tangent * width;
                    Vector3 bottomA = topA - Vector3.up * drop - direction * .9f + tangent * (width * .2f);
                    Vector3 bottomB = topB - Vector3.up * drop - direction * .9f - tangent * (width * .2f);
                    Panel(mesh, topA, topB, bottomB, bottomA);
                }
            }
            return mesh.Build("Свисающая зелень");
        }

        private static Vector2 Axis => new Vector2(Mathf.Cos(RiverAxis), Mathf.Sin(RiverAxis));
        private static Vector2 Perp => new Vector2(-Mathf.Sin(RiverAxis), Mathf.Cos(RiverAxis));

        private static float MainLateral(float u) =>
            RiverOffset + 92f * Mathf.Sin(u / 520f + .4f) + 42f * Mathf.Sin(u / 230f - 1.1f) + 16f * Mathf.Sin(u / 95f);

        private static float BayWidth(float u)
        {
            float width = 0f;
            for (int bay = 0; bay < BayU.Length; bay++)
                width += BaySize[bay] * Mathf.Exp(-Mathf.Pow((u - BayU[bay]) / 250f, 2f));
            return width;
        }

        private static Vector2 MainPoint(float u) => Axis * u + Perp * MainLateral(u);

        private static Vector2 ArmOrigin(int arm) => MainPoint(ArmU[arm]);

        private static Vector2 ArmAxis(int arm) =>
            new Vector2(Mathf.Cos(RiverAxis + ArmBend[arm]), Mathf.Sin(RiverAxis + ArmBend[arm]));

        private static Vector2 ArmPerp(int arm) =>
            new Vector2(-Mathf.Sin(RiverAxis + ArmBend[arm]), Mathf.Cos(RiverAxis + ArmBend[arm]));

        private static float ArmLateral(float v, int arm) =>
            52f * Mathf.Sin(v / 300f + arm) + 24f * Mathf.Sin(v / 120f + 1.7f + arm);

        private static Vector2 ArmPoint(float v, int arm) =>
            ArmOrigin(arm) + ArmAxis(arm) * v + ArmPerp(arm) * ArmLateral(v, arm);

        private static float ArmDistance(float x, float z, int arm)
        {
            Vector2 point = new Vector2(x, z);
            float v = Vector2.Dot(point - ArmOrigin(arm), ArmAxis(arm));
            if (v <= 0f) return Vector2.Distance(point, ArmOrigin(arm));
            if (v >= ArmLength[arm]) return Vector2.Distance(point, ArmPoint(ArmLength[arm], arm));
            return Vector2.Distance(point, ArmPoint(v, arm));
        }

        private static float RiverField(float x, float z, out float width)
        {
            Vector2 point = new Vector2(x, z);
            float u = Vector2.Dot(point, Axis);
            float mainDistance = Vector2.Distance(point, MainPoint(u));
            width = 68f + BayWidth(u) + 7f * Mathf.Sin(u / 70f);
            float best = mainDistance;
            for (int arm = 0; arm < ArmU.Length; arm++)
            {
                float distance = ArmDistance(x, z, arm);
                if (distance < best)
                {
                    best = distance;
                    width = ArmWidth[arm] + 5f * Mathf.Sin(distance * .02f + arm);
                }
            }
            return best;
        }

        private static readonly float[,] ValleyHills =
        {
            { -150f, 680f, 190f, 62f }, { -120f, 780f, 170f, 55f }, { -90f, 660f, 150f, 48f },
            { -60f, 860f, 210f, 70f }, { -30f, 900f, 160f, 52f }, { 0f, 760f, 230f, 84f },
            { 30f, 680f, 150f, 46f }, { 60f, 820f, 200f, 68f }, { 90f, 720f, 170f, 58f },
            { 120f, 950f, 220f, 76f }, { 150f, 700f, 160f, 50f }, { 180f, 880f, 240f, 90f },
            { 210f, 660f, 150f, 48f }, { 240f, 840f, 200f, 66f }, { 270f, 720f, 170f, 56f },
            { 300f, 900f, 220f, 78f }
        };

        private static Mound[] FixedHills()
        {
            var list = new List<Mound>();
            for (int index = 0; index < ValleyHills.GetLength(0); index++)
            {
                float angle = ValleyHills[index, 0] * Mathf.Deg2Rad;
                float radius = ValleyHills[index, 1];
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                if (RiverField(x, z, out _) < 240f) continue;
                list.Add(new Mound
                {
                    X = x, Z = z, Radius = ValleyHills[index, 2], Height = ValleyHills[index, 3]
                });
            }
            return list.ToArray();
        }

        private static Mound[] Mounds()
        {
            if (mounds != null) return mounds;
            var list = new List<Mound>();
            var random = new System.Random(8123);
            for (int attempt = 0; attempt < 60 && list.Count < 16; attempt++)
            {
                float angle = (float)random.NextDouble() * Tau;
                float radius = Mathf.Lerp(470f, 1200f, (float)random.NextDouble());
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float river = RiverField(x, z, out _);
                if (river < 190f) continue;
                list.Add(new Mound
                {
                    X = x,
                    Z = z,
                    Radius = Mathf.Lerp(90f, 230f, (float)random.NextDouble()),
                    Height = Mathf.Lerp(14f, 48f, (float)random.NextDouble())
                });
            }
            list.AddRange(FixedHills());
            mounds = list.ToArray();
            return mounds;
        }

        public static float ValleyHeight(float x, float z)
        {
            float radius = Mathf.Sqrt(x * x + z * z);
            float macro = Mathf.PerlinNoise(x * .0045f + 11f, z * .0045f + 7f) - .5f;
            float fine = Mathf.PerlinNoise(x * .017f + 3f, z * .017f + 19f) - .5f;
            float micro = Mathf.PerlinNoise(x * .052f + 17f, z * .052f + 5f) - .5f;
            float ripple = Mathf.PerlinNoise(x * .114f + 31f, z * .114f + 23f) - .5f;
            float ridge = 1f - Mathf.Abs(macro * 2f);
            float river = RiverField(x, z, out _);
            float corridor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 260f, river));
            float basin = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(330f, 520f, radius));
            float height = (macro * 11f + fine * 2.4f + micro * 1.5f + ripple * .55f) * basin;
            var data = Mounds();
            for (int mound = 0; mound < data.Length; mound++)
            {
                float dx = x - data[mound].X;
                float dz = z - data[mound].Z;
                height += data[mound].Height * Mathf.Exp(-(dx * dx + dz * dz) / (data[mound].Radius * data[mound].Radius) * 2.2f);
            }
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(720f, 1200f, radius));
            height += rise * (16f + ridge * 96f + macro * 30f) * corridor;
            float far = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1250f, 2050f, radius));
            height += far * (34f + ridge * 148f + fine * 34f) * corridor;
            float width;
            float distance = RiverField(x, z, out width);
            float channel = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(width * .4f, width * 1.25f, distance));
            height -= channel * 18f;
            height *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(330f, 430f, radius));
            return height;
        }

        public static Mesh Valley()
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            const int step = 2;
            const int sides = TerraceSides * 2;
            int rows = (ValleyRings.Length - 1) * step + 1;
            Vector3 Point(int row, int side)
            {
                float angle = side * Tau / sides;
                int lowIndex = Mathf.Clamp(row / step, 0, ValleyRings.Length - 1);
                int highIndex = Mathf.Min(ValleyRings.Length - 1, lowIndex + 1);
                float blend = (row - lowIndex * step) / (float)step;
                float ringRadius = Mathf.Lerp(ValleyRings[lowIndex], ValleyRings[highIndex], blend);
                int lower = Mathf.Max(0, lowIndex - 1);
                int upper = Mathf.Min(ValleyRings.Length - 1, highIndex + 1);
                float spacing = (ValleyRings[upper] - ValleyRings[lower]) * .25f;
                float limit = lowIndex < 10 ? 1.7f : lowIndex < 17 ? 3.4f : 17f;
                float jitter = Mathf.Min(limit, spacing * .45f);
                float radius = ringRadius + Mathf.Sin(side * 2.37f + row * 1.9f) * jitter;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                return new Vector3(x, ValleyHeight(x, z), z);
            }
            for (int row = 0; row < rows - 1; row++)
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Vector3 a = Point(row, side);
                    Vector3 b = Point(row, next);
                    Vector3 c = Point(row + 1, side);
                    Vector3 d = Point(row + 1, next);
                    Tri(mesh, a, b, c);
                    Tri(mesh, b, d, c);
                }
            return mesh.Build("Долина у подножия");
        }

        public static Mesh River()
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            const int steps = 320;
            for (int step = 0; step < steps; step++)
            {
                float first = Mathf.Lerp(-2150f, 2150f, step / (float)steps);
                float second = Mathf.Lerp(-2150f, 2150f, (step + 1f) / steps);
                float halfA = (68f + BayWidth(first)) * 1.06f;
                float halfB = (68f + BayWidth(second)) * 1.06f;
                Vector2 pointA = MainPoint(first);
                Vector2 pointB = MainPoint(second);
                Vector2 directionA = (MainPoint(first + 4f) - MainPoint(first - 4f)).normalized;
                Vector2 directionB = (MainPoint(second + 4f) - MainPoint(second - 4f)).normalized;
                Vector2 sideA = new Vector2(-directionA.y, directionA.x);
                Vector2 sideB = new Vector2(-directionB.y, directionB.x);
                Vector3 innerA = new Vector3(pointA.x - sideA.x * halfA, WaterLevel, pointA.y - sideA.y * halfA);
                Vector3 outerA = new Vector3(pointA.x + sideA.x * halfA, WaterLevel, pointA.y + sideA.y * halfA);
                Vector3 innerB = new Vector3(pointB.x - sideB.x * halfB, WaterLevel, pointB.y - sideB.y * halfB);
                Vector3 outerB = new Vector3(pointB.x + sideB.x * halfB, WaterLevel, pointB.y + sideB.y * halfB);
                Tri(mesh, innerA, innerB, outerA);
                Tri(mesh, innerB, outerB, outerA);
            }
            for (int arm = 0; arm < ArmU.Length; arm++)
            {
                const int armSteps = 150;
                for (int step = 0; step < armSteps; step++)
                {
                    float first = Mathf.Lerp(-40f, ArmLength[arm], step / (float)armSteps);
                    float second = Mathf.Lerp(-40f, ArmLength[arm], (step + 1f) / armSteps);
                    Vector2 pointA = ArmPoint(first, arm);
                    Vector2 pointB = ArmPoint(second, arm);
                    Vector2 directionA = (ArmPoint(first + 5f, arm) - ArmPoint(first - 5f, arm)).normalized;
                    Vector2 directionB = (ArmPoint(second + 5f, arm) - ArmPoint(second - 5f, arm)).normalized;
                    Vector2 sideA = new Vector2(-directionA.y, directionA.x);
                    Vector2 sideB = new Vector2(-directionB.y, directionB.x);
                    float halfA = ArmWidth[arm] * 1.12f;
                    float halfB = ArmWidth[arm] * 1.12f;
                    Vector3 innerA = new Vector3(pointA.x - sideA.x * halfA, WaterLevel, pointA.y - sideA.y * halfA);
                    Vector3 outerA = new Vector3(pointA.x + sideA.x * halfA, WaterLevel, pointA.y + sideA.y * halfA);
                    Vector3 innerB = new Vector3(pointB.x - sideB.x * halfB, WaterLevel, pointB.y - sideB.y * halfB);
                    Vector3 outerB = new Vector3(pointB.x + sideB.x * halfB, WaterLevel, pointB.y + sideB.y * halfB);
                    Tri(mesh, innerA, innerB, outerA);
                    Tri(mesh, innerB, outerB, outerA);
                }
            }
            return mesh.Build("Бирюзовая река");
        }

        private static Tree[] Trees()
        {
            if (trees != null) return trees;
            var list = new List<Tree>();
            var random = new System.Random(90210);
            for (int clump = 0; clump < 46; clump++)
            {
                float centerAngle = (float)random.NextDouble() * Tau;
                float centerRadius = Mathf.Lerp(420f, 1150f, (float)random.NextDouble());
                Vector2 candidate = new Vector2(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle)) * centerRadius;
                float river = RiverField(candidate.x, candidate.y, out _);
                if (river < 150f)
                {
                    float shift = 150f - river;
                    candidate += (candidate.normalized) * shift;
                }
                float mask = Mathf.PerlinNoise(candidate.x * .0016f + 41f, candidate.y * .0016f + 17f);
                if (mask < .42f) continue;
                float spread = Mathf.Lerp(34f, 96f, (float)random.NextDouble()) * Mathf.Lerp(.7f, 1.4f, mask);
                int count = Mathf.RoundToInt((12 + random.Next(20)) * Mathf.Lerp(.6f, 1.5f, mask));
                for (int tree = 0; tree < count; tree++)
                {
                    Vector2 offset = new Vector2((float)random.NextDouble() - .5f, (float)random.NextDouble() - .5f) * spread * 2f;
                    Vector2 spot = candidate + offset;
                    float width;
                    if (RiverField(spot.x, spot.y, out width) < width * 1.15f) continue;
                    float edge = offset.magnitude / Mathf.Max(1f, spread);
                    if (Mathf.PerlinNoise(spot.x * .02f + 13f, spot.y * .02f + 29f) < .3f) continue;
                    list.Add(new Tree
                    {
                        Position = new Vector3(spot.x, ValleyHeight(spot.x, spot.y), spot.y),
                        Size = Mathf.Lerp(22f, 36f, (float)random.NextDouble()) * Mathf.Lerp(1f, .7f, Mathf.Clamp01(edge)),
                        Yaw = (float)random.NextDouble() * 360f
                    });
                }
            }
            for (int attempt = 0; attempt < 130; attempt++)
            {
                float angle = (float)random.NextDouble() * Tau;
                float radius = Mathf.Lerp(430f, 1150f, Mathf.Sqrt((float)random.NextDouble()));
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float width;
                if (RiverField(x, z, out width) < width * 1.2f) continue;
                list.Add(new Tree
                {
                    Position = new Vector3(x, ValleyHeight(x, z), z),
                    Size = Mathf.Lerp(14f, 28f, (float)random.NextDouble()),
                    Yaw = (float)random.NextDouble() * 360f
                });
            }
            for (int tier = 0; tier < TierCount - 1; tier++)
                for (int cluster = 0; cluster < 24; cluster++)
                {
                    float edgeAngle = SideAngle(random.Next(Sides)) + Mathf.Sin(cluster * 2.3f + tier) * .02f;
                    if (tier >= 3 && FrontDistance(edgeAngle) < 20f) continue;
                    float mask = Mathf.PerlinNoise(edgeAngle * 2.6f + tier * 4.1f, tier * 2.3f);
                    if (mask < .24f) continue;
                    int count = 6 + Mathf.FloorToInt(mask * 5f);
                    float power = Mathf.Pow((float)random.NextDouble(), 2.2f);
                    for (int tree = 0; tree < count; tree++)
                    {
                        float angle = edgeAngle + ((float)random.NextDouble() - .5f) * .15f;
                        float edgeF = Mathf.Lerp(.06f, .34f, (float)random.NextDouble());
                        float size = 14f + power * 12f + (float)random.NextDouble() * 4f;
                        list.Add(new Tree
                        {
                            Position = ShelfPoint(tier, angle, edgeF) + Vector3.down * 1.6f,
                            Size = size,
                            Yaw = (float)random.NextDouble() * 360f
                        });
                    }
                }
            for (int tier = 0; tier < TierCount - 1; tier++)
            {
                int clusters = WideShelf[tier] > 0 ? 3 + tier % 3 : 1 + tier % 2;
                for (int cluster = 0; cluster < clusters; cluster++)
                {
                    float centerAngle = (float)random.NextDouble() * Tau;
                    if (tier >= 3 && FrontDistance(centerAngle) < 44f) continue;
                    if (Mathf.PerlinNoise(centerAngle * 1.9f + tier, tier * 2.3f) < .34f) continue;
                    float center = WideShelf[tier] > 0
                        ? Mathf.Lerp(.4f, .85f, (float)random.NextDouble())
                        : Mathf.Lerp(.25f, .6f, (float)random.NextDouble());
                    int count = WideShelf[tier] > 0 ? 4 + random.Next(6) : 1 + random.Next(3);
                    for (int tree = 0; tree < count; tree++)
                    {
                        float angle = centerAngle + ((float)random.NextDouble() - .5f) * .24f;
                        float f = Mathf.Clamp(center + ((float)random.NextDouble() - .5f) * .4f, .2f, .94f);
                        list.Add(new Tree
                        {
                            Position = ShelfPoint(tier, angle, f) + Vector3.down * 1.2f,
                            Size = Mathf.Lerp(7f, 15f, (float)random.NextDouble()),
                            Yaw = (float)random.NextDouble() * 360f
                        });
                    }
                }
            }
            for (int tree = 0; tree < 8; tree++)
            {
                float angle = -Mathf.PI * .5f + (tree < 4 ? -1f : 1f) * Mathf.Lerp(.55f, 1.1f, (tree % 4) / 3f);
                if (Mathf.PerlinNoise(angle * 2.7f, tree * 1.3f) < .3f) continue;
                float f = Mathf.Lerp(.28f, .82f, (tree % 4) / 3f);
                list.Add(new Tree
                {
                    Position = ShelfPoint(3, angle, f) + Vector3.down * 1.2f,
                    Size = Mathf.Lerp(10f, 17f, (float)random.NextDouble()),
                    Yaw = (float)random.NextDouble() * 360f
                });
            }
            list.Add(new Tree
            {
                Position = ShelfPoint(3, -122f * Mathf.Deg2Rad, .52f) + Vector3.down * 2.2f,
                Size = 44f,
                Yaw = 18f
            });
            list.Add(new Tree
            {
                Position = ShelfPoint(3, -142f * Mathf.Deg2Rad, .74f) + Vector3.down * 2.2f,
                Size = 36f,
                Yaw = 152f
            });
            list.Add(new Tree
            {
                Position = ShelfPoint(6, 12f * Mathf.Deg2Rad, .62f) + Vector3.down * 1.8f,
                Size = 24f,
                Yaw = 214f
            });
            float[] giantAngle = { -58f, -104f, -150f };
            float[] giantRadius = { 402f, 392f, 416f };
            float[] giantSize = { 29f, 27f, 30f };
            for (int giant = 0; giant < giantAngle.Length; giant++)
            {
                float angle = giantAngle[giant] * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * giantRadius[giant];
                float z = Mathf.Sin(angle) * giantRadius[giant];
                list.Add(new Tree
                {
                    Position = new Vector3(x, ValleyHeight(x, z), z),
                    Size = giantSize[giant],
                    Yaw = giant * 77f
                });
            }
            trees = list.ToArray();
            return trees;
        }

        private static Mesh FirNeedles()
        {
            var mesh = new CrownRockMesh();
            const int sides = 5;
            for (int tier = 0; tier < 8; tier++)
            {
                float bottom = .08f + tier * .105f;
                float top = bottom + .3f;
                float width = .34f * (1f - tier * .1f);
                Vector3 peak = new Vector3(.012f * tier, top, 0f);
                for (int side = 0; side < sides; side++)
                {
                    Vector3 first = FirRim(side, sides, width, bottom, tier);
                    Vector3 second = FirRim(side + 1, sides, width, bottom, tier);
                    Tri(mesh, peak, second, first);
                    Tri(mesh, new Vector3(0f, bottom, 0f), first, second);
                }
            }
            return mesh.Build("Крона ели");
        }

        private static Vector3 FirRim(int index, int sides, float width, float bottom, int tier)
        {
            float angle = index * Tau / sides;
            float radius = width * (1f + .12f * Mathf.Sin(index * 2.3f + tier));
            return new Vector3(Mathf.Cos(angle) * radius, bottom + Mathf.Sin(index * 1.9f + tier) * .02f,
                Mathf.Sin(angle) * radius);
        }

        private static Mesh FirTrunk()
        {
            var mesh = new CrownRockMesh();
            const int sides = 7;
            for (int side = 0; side < sides; side++)
            {
                float first = side * Tau / sides;
                float second = (side + 1) * Tau / sides;
                Vector3 bottomFirst = new Vector3(Mathf.Cos(first) * .5f, 0f, Mathf.Sin(first) * .5f);
                Vector3 bottomSecond = new Vector3(Mathf.Cos(second) * .5f, 0f, Mathf.Sin(second) * .5f);
                Vector3 topFirst = new Vector3(Mathf.Cos(first) * .34f, 1f, Mathf.Sin(first) * .34f);
                Vector3 topSecond = new Vector3(Mathf.Cos(second) * .34f, 1f, Mathf.Sin(second) * .34f);
                Tri(mesh, bottomFirst, topFirst, bottomSecond);
                Tri(mesh, bottomSecond, topFirst, topSecond);
            }
            return mesh.Build("Ствол ели");
        }

        public static Mesh Forest(bool trunks)
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            Mesh source = trunks ? FirTrunk() : FirNeedles();
            var data = Trees();
            for (int index = 0; index < data.Length; index++)
            {
                Tree tree = data[index];
                Matrix4x4 matrix = trunks
                    ? Matrix4x4.TRS(tree.Position, Quaternion.Euler(0f, tree.Yaw, 0f),
                        new Vector3(tree.Size * .1f, tree.Size * .45f, tree.Size * .1f))
                    : Matrix4x4.TRS(tree.Position + Vector3.up * (tree.Size * .04f), Quaternion.Euler(0f, tree.Yaw, 0f),
                        Vector3.one * tree.Size);
                Append(mesh, source, matrix);
            }
            return mesh.Build(trunks ? "Стволы елей" : "Кроны елей");
        }

        public static Mesh Boulders()
        {
            var mesh = new CrownRockMesh();
            shade = .92f;
            var random = new System.Random(4071);
            for (int stone = 0; stone < 70; stone++)
            {
                float angle = (float)random.NextDouble() * Tau;
                float radius = Mathf.Lerp(410f, 700f, (float)random.NextDouble());
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float width;
                if (RiverField(x, z, out width) < width * 1.2f) continue;
                Vector3 size = new Vector3(4f + (float)random.NextDouble() * 10f, 3f + (float)random.NextDouble() * 9f,
                    4f + (float)random.NextDouble() * 10f);
                Chunk(mesh, new Vector3(x, ValleyHeight(x, z), z) + Vector3.up * size.y * .3f, size, stone);
            }
            for (int stone = 0; stone < 38; stone++)
            {
                int tier = stone % TierCount;
                float angle = (float)random.NextDouble() * Tau;
                if (tier >= 3 && FrontDistance(angle) < 34f) continue;
                float f = Mathf.Lerp(.2f, .88f, (float)random.NextDouble());
                float scale = 1.4f + (float)random.NextDouble() * 3f;
                Vector3 position = ShelfPoint(tier, angle, f);
                Chunk(mesh, position + Vector3.up * scale * .28f,
                    new Vector3(scale, scale * .8f, scale), stone + 71);
            }
            return mesh.Build("Валуны долины");
        }

        private static Ridge[] Ridges()
        {
            if (ridges != null) return ridges;
            var list = new List<Ridge>();
            var random = new System.Random(6137);
            for (int ridge = 0; ridge < 9; ridge++)
                list.Add(new Ridge
                {
                    Angle = ridge * Tau / 9f + ((float)random.NextDouble() - .5f) * .5f,
                    Distance = Mathf.Lerp(1550f, 2100f, (float)random.NextDouble()),
                    Span = Mathf.Lerp(1.0f, 1.7f, (float)random.NextDouble()),
                    Height = Mathf.Lerp(170f, 330f, (float)random.NextDouble()),
                    Width = Mathf.Lerp(190f, 340f, (float)random.NextDouble()),
                    Twist = (float)random.NextDouble() * 10f
                });
            ridges = list.ToArray();
            return ridges;
        }

        public static Mesh FarPeaks(bool snow)
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            var data = Ridges();
            for (int ridge = 0; ridge < data.Length; ridge++) RidgeWall(mesh, data[ridge], snow);
            return Soften(mesh.Build(snow ? "Снежные шапки хребтов" : "Дальние хребты"));
        }

        private static Mesh Soften(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var groups = new Dictionary<Vector3, List<int>>();
            for (int index = 0; index < vertices.Length; index++)
            {
                if (!groups.TryGetValue(vertices[index], out var list))
                {
                    list = new List<int>();
                    groups.Add(vertices[index], list);
                }
                list.Add(index);
            }
            var result = new Vector3[normals.Length];
            for (int index = 0; index < normals.Length; index++) result[index] = normals[index];
            foreach (var group in groups.Values)
            {
                Vector3 sample = vertices[group[0]];
                Vector3 outward = new Vector3(sample.x, 0f, sample.z);
                outward = outward.sqrMagnitude > .001f ? outward.normalized : Vector3.up;
                Vector3 average = Vector3.zero;
                foreach (int index in group)
                {
                    Vector3 normal = normals[index];
                    if (Vector3.Dot(normal, outward) > 0f) normal = -normal;
                    average += normal;
                }
                if (average.sqrMagnitude < .0001f) average = -outward;
                average.Normalize();
                foreach (int index in group) result[index] = average;
            }
            mesh.normals = result;
            return mesh;
        }

        private static float RidgeProfile(float t, float twist)
        {
            float value = .22f;
            for (int bump = 1; bump <= 3; bump++)
            {
                float center = .14f + .33f * bump + Mathf.Sin(twist * bump) * .08f;
                float height = .5f + .5f * Mathf.Abs(Mathf.Sin(twist * 1.7f * bump));
                value += height * Mathf.Exp(-Mathf.Pow((t - center) / .19f, 2f));
            }
            return Mathf.Clamp01(value);
        }

        private static void RidgeWall(CrownRockMesh mesh, Ridge ridge, bool snow)
        {
            int samples = Mathf.Clamp(Mathf.RoundToInt(ridge.Span / .07f), 10, 40);
            var left = new Vector3[samples];
            var leftMid = new Vector3[samples];
            var crest = new Vector3[samples];
            var rightMid = new Vector3[samples];
            var right = new Vector3[samples];
            for (int sample = 0; sample < samples; sample++)
            {
                float t = sample / (samples - 1f);
                float angle = ridge.Angle + (t - .5f) * ridge.Span;
                float distance = ridge.Distance + Mathf.Sin(t * 6.4f + ridge.Twist) * 90f;
                float profile = RidgeProfile(t, ridge.Twist);
                float crestHeight = ridge.Height * profile;
                float halfWidth = ridge.Width * (.45f + .55f * profile);
                float x = Mathf.Cos(angle) * distance;
                float z = Mathf.Sin(angle) * distance;
                float baseY = ValleyHeight(x, z) - 26f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 foot = new Vector3(x, baseY, z);
                left[sample] = foot - outward * halfWidth + Vector3.up * (crestHeight * .1f);
                leftMid[sample] = foot - outward * (halfWidth * .45f) + Vector3.up * (crestHeight * .66f);
                crest[sample] = foot + Vector3.up * crestHeight + new Vector3(Mathf.Sin(t * 6.5f + ridge.Twist) * 8f, 0f,
                    Mathf.Cos(t * 5.5f - ridge.Twist) * 8f);
                rightMid[sample] = foot + outward * (halfWidth * .45f) + Vector3.up * (crestHeight * .66f);
                right[sample] = foot + outward * halfWidth + Vector3.up * (crestHeight * .1f);
            }
            for (int sample = 0; sample < samples - 1; sample++)
            {
                float tone = .94f + .06f * Mathf.Sin(sample * 1.7f + ridge.Twist);
                if (snow)
                {
                    Vector3 crestA = crest[sample] + Vector3.up * 2f;
                    Vector3 crestB = crest[sample + 1] + Vector3.up * 2f;
                    float snowA = .1f + .07f * Mathf.Sin(sample * 2.3f + ridge.Twist);
                    float snowB = .1f + .07f * Mathf.Sin((sample + 1) * 2.3f + ridge.Twist);
                    Vector3 leftA = Vector3.Lerp(leftMid[sample], crest[sample], snowA) + Vector3.up * 2f;
                    Vector3 leftB = Vector3.Lerp(leftMid[sample + 1], crest[sample + 1], snowB) + Vector3.up * 2f;
                    Vector3 rightA = Vector3.Lerp(rightMid[sample], crest[sample], snowA) + Vector3.up * 2f;
                    Vector3 rightB = Vector3.Lerp(rightMid[sample + 1], crest[sample + 1], snowB) + Vector3.up * 2f;
                    shade = .94f * tone;
                    Panel(mesh, leftA, crestA, crestB, leftB);
                    shade = 1f * tone;
                    Panel(mesh, crestA, rightA, rightB, crestB);
                    continue;
                }
                shade = .86f * tone;
                Panel(mesh, left[sample], leftMid[sample], leftMid[sample + 1], left[sample + 1]);
                shade = .93f * tone;
                Panel(mesh, leftMid[sample], crest[sample], crest[sample + 1], leftMid[sample + 1]);
                shade = .98f * tone;
                Panel(mesh, crest[sample], rightMid[sample], rightMid[sample + 1], crest[sample + 1]);
                shade = .9f * tone;
                Panel(mesh, rightMid[sample], right[sample], right[sample + 1], rightMid[sample + 1]);
            }
        }

        public static Mesh Talus()
        {
            var mesh = new CrownRockMesh();
            shade = .9f;
            var random = new System.Random(3317);
            for (int tier = 1; tier < TierCount; tier++)
            {
                for (int side = 0; side < Sides; side++)
                {
                    float first = SideAngle(side);
                    float second = SideAngle(side + 1);
                    float drop = 1.2f + .09f * (WallY[tier] - BaseY[tier]);
                    Vector3 innerA = WallPoint(tier, first, .05f);
                    Vector3 innerB = WallPoint(tier, second, .05f);
                    Vector3 outerA = ShelfPoint(tier - 1, first, .48f);
                    Vector3 outerB = ShelfPoint(tier - 1, second, .48f);
                    Vector3 midA = Vector3.Lerp(innerA, outerA, .45f) + Vector3.up * (drop * .55f);
                    Vector3 midB = Vector3.Lerp(innerB, outerB, .45f) + Vector3.up * (drop * .55f);
                    Tri(mesh, innerA, innerB, midA);
                    Tri(mesh, innerB, midB, midA);
                    Tri(mesh, midA, midB, outerA);
                    Tri(mesh, midB, outerB, outerA);
                }
                for (int stone = 0; stone < 9; stone++)
                {
                    float angle = (float)random.NextDouble() * Tau;
                    float nominal = Mathf.Lerp(WallY[tier - 1], BaseY[tier], .35f);
                    Vector3 surface = SurfacePoint(angle, nominal);
                    float scale = 2.4f + (float)random.NextDouble() * 6.2f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Chunk(mesh, new Vector3(surface.x, surface.y, surface.z) + direction * (1f + (float)random.NextDouble() * 6f)
                        + Vector3.up * (scale * .3f), new Vector3(scale, scale * .7f, scale), stone + tier * 37);
                }
            }
            float mouthY = WallPoint(CaveTier, SideAngle(MainSide), 0f).y;
            float mouthRadius = FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f));
            shade = .74f;
            for (int stone = 0; stone < 78; stone++)
            {
                float angle = SideAngle(MainSide) + ((float)random.NextDouble() - .5f) * .72f;
                float distance = 2f + (float)random.NextDouble() * 54f;
                float scale = 2.6f + (float)random.NextDouble() * 7.4f * Mathf.Lerp(1f, .4f, distance / 44f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 center = new Vector3(direction.x * (mouthRadius + distance), mouthY - 1.4f - distance * .06f,
                    direction.z * (mouthRadius + distance));
                Chunk(mesh, center + Vector3.up * (scale * .3f), new Vector3(scale, scale * .75f, scale * 1.1f), stone + 411);
            }
            return mesh.Build("Осыпные шлейфы");
        }

        public static Mesh Collapse()
        {
            var mesh = new CrownRockMesh();
            shade = .95f;
            var random = new System.Random(5177);
            for (int breach = 0; breach < BreachAngle.Length; breach++)
            {
                float crest = WallY[BreachTier[breach]];
                for (int stone = 0; stone < 36; stone++)
                {
                    float angle = BreachAngle[breach] + ((float)random.NextDouble() - .5f) * BreachWidth[breach] * 3.4f;
                    float nominal = crest - (float)random.NextDouble() * (crest - BaseY[BreachTier[breach]] + 26f);
                    Vector3 surface = SurfacePoint(angle, nominal);
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float scale = 3.2f + (float)random.NextDouble() * 8.4f;
                    Vector3 center = new Vector3(surface.x, surface.y, surface.z)
                        + direction * (2f + (float)random.NextDouble() * 24f) + Vector3.up * (scale * .3f);
                    Chunk(mesh, center, new Vector3(scale, scale * .8f, scale * 1.15f), stone + breach * 101);
                }
            }
            return mesh.Build("Обвалы ярусов");
        }

        public static Mesh RockField(IList<Vector3> centers, IList<float> scales, int seed)
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            var random = new System.Random(seed);
            for (int index = 0; index < centers.Count; index++)
            {
                float scale = scales[Mathf.Min(index, scales.Count - 1)];
                Chunk(mesh, centers[index] + Vector3.up * scale * .3f, new Vector3(scale, scale * .85f, scale * 1.15f),
                    seed + index * 17);
                for (int satellite = 0; satellite < 3; satellite++)
                {
                    float size = scale * Mathf.Lerp(.34f, .6f, (float)random.NextDouble());
                    Vector3 offset = new Vector3(((float)random.NextDouble() - .5f) * scale * 1.1f, 0f,
                        ((float)random.NextDouble() - .5f) * scale * 1.1f);
                    Chunk(mesh, centers[index] + offset + Vector3.up * size * .3f,
                        new Vector3(size * (.85f + .3f * (float)random.NextDouble()),
                            size * (.7f + .35f * (float)random.NextDouble()), size),
                        seed + index * 31 + satellite);
                }
            }
            return mesh.Build("Камни-ориентиры тропы");
        }

        public static Mesh ValleyOutcrops()
        {
            var mesh = new CrownRockMesh();
            shade = 1f;
            var hills = FixedHills();
            var random = new System.Random(9241);
            for (int index = 0; index < hills.Length; index++)
            {
                Mound hill = hills[index];
                Vector3 foot = new Vector3(hill.X, ValleyHeight(hill.X, hill.Z), hill.Z);
                float scale = hill.Radius * .16f;
                int pillars = 2 + random.Next(2);
                for (int pillar = 0; pillar < pillars; pillar++)
                {
                    float bearing = (float)random.NextDouble() * Tau;
                    float offset = hill.Radius * (.1f + .22f * (float)random.NextDouble());
                    float height = hill.Height * Mathf.Lerp(.45f, 1f, (float)random.NextDouble());
                    Vector3 basePoint = foot + new Vector3(Mathf.Cos(bearing) * offset, 0f, Mathf.Sin(bearing) * offset);
                    shade = Mathf.Lerp(.82f, 1f, (float)random.NextDouble());
                    Chunk(mesh, basePoint + Vector3.up * (height * .34f),
                        new Vector3(scale * (.75f + .35f * (float)random.NextDouble()), height,
                            scale * (.7f + .4f * (float)random.NextDouble())), index * 37 + pillar * 11 + 5);
                    if (pillar == 0)
                    {
                        float buddy = scale * .48f;
                        Chunk(mesh, basePoint + new Vector3(scale * .55f, height * .18f, -scale * .4f),
                            new Vector3(buddy, height * .62f, buddy * 1.1f), index * 53 + 17);
                    }
                }
                for (int stone = 0; stone < 3; stone++)
                {
                    float bearing = (float)random.NextDouble() * Tau;
                    float distance = hill.Radius * (.45f + .5f * (float)random.NextDouble());
                    Vector3 point = foot + new Vector3(Mathf.Cos(bearing) * distance, 0f, Mathf.Sin(bearing) * distance);
                    float size = scale * (.35f + .45f * (float)random.NextDouble());
                    Chunk(mesh, point + Vector3.up * (size * .28f),
                        new Vector3(size, size * .72f, size * 1.08f), index * 61 + stone * 7 + 3);
                }
            }
            shade = 1f;
            return mesh.Build("Останцы долины");
        }

        public static Mesh JumpCourse(IList<Vector3> centers, IList<float> yaws, IList<float> sizes, int seed)
        {
            var mesh = new CrownRockMesh();
            for (int index = 0; index < centers.Count; index++)
            {
                Vector3 center = centers[index];
                float yaw = yaws[Mathf.Min(index, yaws.Count - 1)];
                float size = sizes[Mathf.Min(index, sizes.Count - 1)];
                JumpStone(mesh, center, yaw, size, seed + index * 17);
            }
            return mesh.Build("Прыжковые площадки");
        }

        private static void JumpStone(CrownRockMesh mesh, Vector3 top, float yaw, float diameter, int seed)
        {
            const int sides = 10;
            float radius = diameter * .5f;
            float height = diameter * .68f;
            Quaternion spin = Quaternion.Euler(0f, yaw, 0f);
            var upper = new Vector3[sides];
            var waist = new Vector3[sides];
            var lower = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Tau / sides;
                float wobble = .9f + .1f * Hash(seed * 2.3f + side * 1.7f);
                Vector3 spoke = spin * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * wobble;
                upper[side] = top + spoke;
                waist[side] = top + spoke * 1.06f - Vector3.up * (height * .52f);
                lower[side] = top + spoke * .92f - Vector3.up * height;
            }
            shade = Mathf.Lerp(.8f, .87f, Hash(seed * 1.9f + 3.1f));
            for (int side = 0; side < sides; side++) Tri(mesh, upper[side], top, upper[(side + 1) % sides]);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                shade = .68f;
                Panel(mesh, waist[side], upper[side], upper[next], waist[next]);
                shade = .59f;
                Panel(mesh, lower[side], waist[side], waist[next], lower[next]);
            }
            shade = .52f;
            Vector3 baseCenter = top - Vector3.up * height;
            for (int side = 0; side < sides; side++) Tri(mesh, lower[(side + 1) % sides], baseCenter, lower[side]);
        }

        public static Mesh TrailStones(IList<Vector3> points, float width)
        {
            var mesh = new CrownRockMesh();
            var random = new System.Random(1234);
            float accumulator = 4.5f;
            for (int index = 1; index < points.Count; index++)
            {
                Vector3 first = points[index - 1], second = points[index];
                float segment = Vector3.Distance(first, second);
                Vector3 forward = second - first;
                forward.y = 0f;
                if (forward.sqrMagnitude < .0001f) continue;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                accumulator += segment;
                while (accumulator >= 9f)
                {
                    accumulator -= 9f;
                    float t = Mathf.Clamp01(1f - accumulator / Mathf.Max(segment, .001f));
                    Vector3 point = Vector3.Lerp(first, second, t)
                        + side * ((random.NextDouble() > .5 ? 1f : -1f) * (width * .5f + 1.2f));
                    float slope = Mathf.Max(
                        Mathf.Abs(GroundHeight(point + new Vector3(.9f, 0f, 0f)) - GroundHeight(point - new Vector3(.9f, 0f, 0f))),
                        Mathf.Abs(GroundHeight(point + new Vector3(0f, 0f, .9f)) - GroundHeight(point - new Vector3(0f, 0f, .9f))));
                    if (slope > 1.2f) continue;
                    float scale = .8f + (float)random.NextDouble() * 1f;
                    point.y = GroundHeight(point) + .18f;
                    shade = Mathf.Lerp(.86f, 1f, (float)random.NextDouble());
                    Chunk(mesh, point, new Vector3(scale, scale * .45f, scale * .8f), index * 13 + 3);
                }
            }
            shade = 1f;
            return mesh.Build("Камни по краю тропы");
        }

        public static Mesh CheckpointStone()
        {
            var mesh = new CrownRockMesh();
            shade = .96f;
            Chunk(mesh, new Vector3(0f, 2.3f, 0f), new Vector3(4.8f, 4.2f, 4.2f), 91);
            Chunk(mesh, new Vector3(.2f, 5.4f, -.2f), new Vector3(3.1f, 2.9f, 2.9f), 137);
            Chunk(mesh, new Vector3(-.2f, 7.4f, .1f), new Vector3(1.9f, 1.8f, 1.8f), 211);
            shade = 1f;
            return mesh.Build("Камень-чекпоинт");
        }

        public static Mesh WaypointStuds(IList<Vector3> points)
        {
            var mesh = new CrownRockMesh();
            shade = .9f;
            for (int index = 0; index < points.Count; index++)
            {
                Vector3 point = points[index];
                float slope = Mathf.Max(
                    Mathf.Abs(GroundHeight(point + new Vector3(.9f, 0f, 0f)) - GroundHeight(point - new Vector3(.9f, 0f, 0f))),
                    Mathf.Abs(GroundHeight(point + new Vector3(0f, 0f, .9f)) - GroundHeight(point - new Vector3(0f, 0f, .9f))));
                if (slope > 1.2f) continue;
                Vector3 direction = points[Mathf.Min(index + 1, points.Count - 1)] - points[Mathf.Max(index - 1, 0)];
                direction.y = 0f;
                if (direction.sqrMagnitude < .0001f) direction = Vector3.forward;
                direction.Normalize();
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                Quaternion spin = Quaternion.Euler(0f, yaw, 0f);
                Vector3 basePoint = point + Vector3.up * .04f;
                const int sides = 6;
                var rim = new Vector3[sides];
                var foot = new Vector3[sides];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Tau / sides;
                    float radius = .36f + .05f * Hash(index * 1.7f + side * 3.1f);
                    Vector3 spoke = spin * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    rim[side] = basePoint + spoke;
                    foot[side] = point - Vector3.up * .22f + spoke * .86f;
                }
                Vector3 crown = basePoint + Vector3.up * .06f;
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    Tri(mesh, rim[side], crown, rim[next]);
                    Panel(mesh, foot[side], rim[side], rim[next], foot[next]);
                }
            }
            shade = 1f;
            return mesh.Build("Отметки вейпоинтов");
        }

        public static float GroundHeight(Vector3 point) => GroundHeight(point, true);

        public static float OuterSurfaceHeight(Vector3 point) => GroundHeight(point, false);

        private static float GroundHeight(Vector3 point, bool tunnel)
        {
            if (tunnel && TryTunnelFloor(point, out float tunnelFloor)) return tunnelFloor;
            float radius = Mathf.Sqrt(point.x * point.x + point.z * point.z);
            float angle = Mathf.Atan2(point.z, point.x);
            if (tunnel && CaveInside(radius, angle) && point.y < CaveCeilingHeight(0f)) return CaveFloorHeight(0f);
            float baseRadius = TierR[0] * Shape(angle) * Asym(angle, 0f) + Flute(angle, 0f) + Cut(angle, 0f)
                + BreachDent(angle, 0f);
            if (radius >= baseRadius) return ValleyHeight(point.x, point.z);
            float low = 0f, high = PeakHeight + 40f;
            for (int step = 0; step < 28; step++)
            {
                float mid = (low + high) * .5f;
                if (FlatRadius(SurfacePoint(angle, mid)) > radius) low = mid;
                else high = mid;
            }
            return SurfacePoint(angle, (low + high) * .5f).y;
        }

        public static float CaveCeilingHeight(float angleOffsetDegrees)
        {
            float angle = SideAngle(MainSide) + angleOffsetDegrees * Mathf.Deg2Rad;
            float nominal = Mathf.Lerp(BaseY[CaveTier + 1], WallY[CaveTier + 1], CaveCeilingU);
            return (nominal + TiltAt(nominal, angle) + BreachSag(angle, nominal)) * HeightScale;
        }

        private static bool CaveInside(float radius, float angle)
        {
            if (Mathf.Abs(AngleDelta(angle, SideAngle(MainSide))) > MainHalf[0] * Tau / Sides) return false;
            float mouthRadius = FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f));
            return radius <= mouthRadius + 3f && radius >= mouthRadius - 140f;
        }

        public static float CaveFloorHeight(float angleOffsetDegrees)
        {
            float angle = SideAngle(MainSide) + angleOffsetDegrees * Mathf.Deg2Rad;
            float nominal = BaseY[CaveTier];
            return (nominal + TiltAt(nominal, angle) + BreachSag(angle, nominal)) * HeightScale;
        }

        public static Vector3 CaveMouth(float angleOffsetDegrees, float inward)
        {
            float angle = SideAngle(MainSide) + angleOffsetDegrees * Mathf.Deg2Rad;
            float mouthRadius = FlatRadius(WallPoint(CaveTier, SideAngle(MainSide), 0f));
            float radius = Mathf.Max(15f, mouthRadius - inward);
            return new Vector3(Mathf.Cos(angle) * radius, CaveFloorHeight(angleOffsetDegrees) + inward * .015f,
                Mathf.Sin(angle) * radius);
        }

        public static float WallHeight(int tier, float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            float high = WallY[tier] + TiltAt(WallY[tier], angle) + BreachSag(angle, WallY[tier]);
            float low = BaseY[tier] + TiltAt(BaseY[tier], angle) + BreachSag(angle, BaseY[tier]);
            return (high - low) * HeightScale;
        }

        public static float LowestCrossing(int tier)
        {
            float best = 0f, lowest = 99999f;
            for (int step = 0; step < 360; step += 2)
            {
                float height = WallHeight(tier, step);
                if (height < lowest) { lowest = height; best = step; }
            }
            return best;
        }

        public static Vector3 TierLedge(int tier, float angleDegrees, float f) =>
            ShelfPoint(tier, angleDegrees * Mathf.Deg2Rad, f);

        public static Vector3 TierFace(int tier, float angleDegrees, float u) =>
            WallPoint(tier, angleDegrees * Mathf.Deg2Rad, u);

        public static Vector3 SummitPoint(float angleDegrees, float nominal) =>
            SurfacePoint(angleDegrees * Mathf.Deg2Rad, nominal);

        public static Vector3 BankPoint(float angleDegrees, float inward)
        {
            float target = angleDegrees * Mathf.Deg2Rad;
            float best = 0f, bestDelta = 999f;
            for (float u = -1400f; u <= 1400f; u += 15f)
            {
                Vector2 probe = MainPoint(u);
                float delta = Mathf.Abs(AngleDelta(Mathf.Atan2(probe.y, probe.x), target));
                if (delta < bestDelta) { bestDelta = delta; best = u; }
            }
            Vector2 point = MainPoint(best);
            Vector2 spot = point - point.normalized * inward;
            return new Vector3(spot.x, ValleyHeight(spot.x, spot.y), spot.y);
        }

        public static Vector3 ValleyPoint(float angleDegrees, float radius)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            return new Vector3(x, ValleyHeight(x, z), z);
        }

        private static void FlatStone(CrownRockMesh mesh, Vector3 center, float radius, float top, int seed)
        {
            const int sides = 8;
            Quaternion spin = Quaternion.Euler(0f, Hash(seed * 3.7f + 1.3f) * 360f, 0f);
            var upper = new Vector3[sides];
            var lower = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Tau / sides;
                float wobble = .94f + .12f * Hash(side * 2.1f + seed * 1.9f);
                upper[side] = new Vector3(Mathf.Cos(angle) * radius * wobble, 0f, Mathf.Sin(angle) * radius * wobble);
                upper[side] = center + spin * upper[side] + Vector3.up * (top - center.y);
                lower[side] = center + spin * new Vector3(Mathf.Cos(angle) * radius * 1.14f,
                    -radius * 1.5f, Mathf.Sin(angle) * radius * 1.14f);
            }
            Vector3 crown = new Vector3(center.x, top, center.z);
            shade = .92f;
            for (int side = 0; side < sides; side++) Tri(mesh, upper[side], crown, upper[(side + 1) % sides]);
            shade = .6f;
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Panel(mesh, lower[side], upper[side], upper[next], lower[next]);
            }
        }

        private static void Chunk(CrownRockMesh mesh, Vector3 center, Vector3 size, int seed)
        {
            const int sides = 9;
            int lobes = Hash(seed * 1.37f + 2.5f) > .5f ? 3 : 2;
            float spin = Hash(seed * 2.11f + 6.7f) * Tau;
            Quaternion tilt = Quaternion.Euler((Hash(seed * 3.53f + 1.2f) - .5f) * 16f,
                (Hash(seed * 4.19f + 8.8f) - .5f) * 360f, (Hash(seed * 5.77f + 3.9f) - .5f) * 16f);
            float Outline(float theta)
            {
                float radius = 0f;
                for (int lobe = 0; lobe < lobes; lobe++)
                {
                    float phase = spin + lobe * Tau / lobes + (Hash(seed * 6.31f + lobe * 2.7f) - .5f) * .9f;
                    float reach = .2f + .2f * Hash(seed * 7.13f + lobe * 3.9f);
                    float spread = .5f + .26f * Hash(seed * 8.27f + lobe * 4.1f);
                    float sine = Mathf.Sin(theta - phase);
                    float root = spread * spread - reach * reach * sine * sine;
                    radius = Mathf.Max(radius, reach * Mathf.Cos(theta - phase) + Mathf.Sqrt(root));
                }
                radius += .15f * (Mathf.PerlinNoise(theta * 2.4f + seed * 1.9f, seed * .7f) - .5f)
                    + .08f * (Mathf.PerlinNoise(theta * 5.8f + seed * 3.7f, seed * 1.3f + 4.4f) - .5f)
                    - .12f * Mathf.Pow(Mathf.Max(0f, Mathf.PerlinNoise(theta * 3.3f + seed * 2.7f,
                        seed * 1.7f + 6.1f) - .68f) / .32f, 2f);
                return Mathf.Max(.26f, radius);
            }
            var lower = new Vector3[sides];
            var middle = new Vector3[sides];
            var upper = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float theta = side * Tau / sides;
                float radius = Outline(theta);
                float fine = Mathf.PerlinNoise(side * 1.31f + seed * 2.17f, seed * .91f) - .5f;
                lower[side] = center + tilt * new Vector3(Mathf.Cos(theta) * size.x * radius * .82f,
                    -size.y * .5f, Mathf.Sin(theta) * size.z * radius * .82f);
                middle[side] = center + tilt * new Vector3(Mathf.Cos(theta) * size.x * radius,
                    size.y * (.05f + .16f * fine), Mathf.Sin(theta) * size.z * radius);
                float pinch = Mathf.Max(.3f,
                    radius * (.55f + .22f * Mathf.PerlinNoise(side * 2.13f + seed * 3.41f, seed * 1.7f)));
                upper[side] = center + tilt * new Vector3(Mathf.Cos(theta) * size.x * pinch,
                    size.y * .5f, Mathf.Sin(theta) * size.z * pinch);
            }
            Vector3 apex = center + tilt * Vector3.up * (size.y * (.56f + .1f * Hash(seed * 9.1f + 4.4f)));
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Panel(mesh, lower[side], middle[side], middle[next], lower[next]);
                Panel(mesh, middle[side], upper[side], upper[next], middle[next]);
                Twin(mesh, upper[side], apex, upper[next]);
            }
        }

        private static float FlatRadius(Vector3 point) => Mathf.Sqrt(point.x * point.x + point.z * point.z);

        private static float FrontDistance(float angle) => Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, -90f));

        private static float AngleDelta(float first, float second) =>
            Mathf.DeltaAngle(first * Mathf.Rad2Deg, second * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        private static Vector3 Polar(float angle, float radius, float y) =>
            new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);

        private static Vector3 Inward(Vector3 point, float depth)
        {
            float length = Mathf.Sqrt(point.x * point.x + point.z * point.z);
            return point - new Vector3(point.x / Mathf.Max(length, .001f), 0f, point.z / Mathf.Max(length, .001f)) * depth;
        }

        private static Vector3 Pinch(Vector3 point, float angle, float amount)
        {
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float along = point.x * direction.x + point.z * direction.z;
            Vector3 lateral = new Vector3(point.x - direction.x * along, 0f, point.z - direction.z * along);
            return new Vector3(direction.x * along + lateral.x * (1f - amount), point.y,
                direction.z * along + lateral.z * (1f - amount));
        }

        private static void Append(CrownRockMesh mesh, Mesh source, Matrix4x4 matrix)
        {
            Vector3[] vertices = source.vertices;
            int[] triangles = source.triangles;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                mesh.Triangle(matrix.MultiplyPoint3x4(vertices[triangles[index]]),
                    matrix.MultiplyPoint3x4(vertices[triangles[index + 1]]),
                    matrix.MultiplyPoint3x4(vertices[triangles[index + 2]]), Rock, Rock, Rock);
            }
        }

        private static void Tri(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
            => mesh.Triangle(a, b, c, new Color(shade, 0f, 0f, 1f), new Color(shade, 0f, 0f, 1f), new Color(shade, 0f, 0f, 1f));

        private static void Twin(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c)
        {
            Tri(mesh, a, b, c);
            Tri(mesh, c, b, a);
        }

        private static void Panel(CrownRockMesh mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(mesh, a, b, c);
            Tri(mesh, a, c, d);
            Tri(mesh, c, b, a);
            Tri(mesh, d, c, a);
        }
    }
}
