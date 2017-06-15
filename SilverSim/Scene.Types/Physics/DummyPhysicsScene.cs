// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Mesh;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsScene : IPhysicsScene
    {
        private SceneInterface m_Scene;
        private readonly UUID m_SceneID;
        private readonly RwLockedList<IObject> m_Agents = new RwLockedList<IObject>();

        public DummyPhysicsScene(SceneInterface scene)
        {
            m_SceneID = scene.ID;
            m_Scene = scene;
        }

        public void Add(IObject obj)
        {
            if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                var agent = (IAgent)obj;
                agent.PhysicsActors.Add(m_SceneID, new AgentUfoPhysics(agent, m_SceneID));
                m_Agents.Add(obj);
            }
        }

        public void Remove(IObject obj)
        {
            if (obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                var agent = (IAgent)obj;
                IPhysicsObject physobj;
                m_Agents.Remove(agent);
                agent.PhysicsActors.Remove(m_SceneID, out physobj);
            }
        }

        public void Shutdown()
        {
            foreach (var obj in m_Agents)
            {
                Remove(obj);
            }
            m_Scene = null;
        }

        public void RemoveAll()
        {
            foreach(var obj in m_Agents)
            {
                Remove(obj);
            }
        }

        public string PhysicsEngineName => "Dummy";

        public double PhysicsDilationTime => 0;

        public double PhysicsExecutionTime => 0f;

        public double PhysicsFPS => 0;

        public uint PhysicsFrameNumber => 0;

        public RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld) =>
            ClosestRayTest(rayFromWorld, rayToWorld, RayTestHitFlags.All);

        public RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld) =>
            AllHitsRayTest(rayFromWorld, rayToWorld, RayTestHitFlags.All);

        public RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags) =>
            AllHitsRayTest(rayFromWorld, rayToWorld, flags, 1);

        public RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags) =>
            AllHitsRayTest(rayFromWorld, rayToWorld, flags, UInt32.MaxValue);

        public RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags, uint maxHits)
        {
            var results = new List<RayResult>();
            RayData ray = new RayData(rayFromWorld, rayToWorld);

            if((flags & RayTestHitFlags.Avatar) != 0)
            {
                GetAgentMatches(ray, flags, results);
            }

            if((flags & (RayTestHitFlags.NonPhantom | RayTestHitFlags.Phantom | RayTestHitFlags.NonPhantom | RayTestHitFlags.Phantom | RayTestHitFlags.Character)) != 0)
            {
                GetObjectMatches(ray, flags, results);
            }

            results.Sort((r1, r2) => (r1.HitPointWorld - rayFromWorld).Length.CompareTo(r2.HitPointWorld - rayFromWorld));

            if(results.Count > maxHits)
            {
                results.RemoveRange((int)maxHits, results.Count - (int)maxHits);
            }

            return results.ToArray();
        }

        public Dictionary<uint, double> GetTopColliders() => new Dictionary<uint, double>();

        private sealed class RayData
        {
            public Vector3 Origin;
            public Vector3 Direction;
            public Vector3 InvDirection;
            public double RayLength;

            public RayData(Vector3 rayFromWorld, Vector3 rayToWorld)
            {
                Origin = rayFromWorld;
                Direction = rayToWorld - rayFromWorld;
                RayLength = Direction.Length;
                Direction = Direction.Normalize();
                InvDirection = -Direction;
            }
        }

        private double IntersectTri(RayData ray, Vector3 tri1, Vector3 tri2, Vector3 tri3, out Vector3 normal)
        {
            Vector3 vec1Proj = tri2 - tri1;
            Vector3 vec2Proj = tri3 - tri2;
            Vector3 vec3Proj = tri1 - tri3;

            normal = vec1Proj.Cross(vec2Proj);

            double div = ray.Direction.Dot(normal);
            if(Math.Abs(div) < double.Epsilon)
            {
                return -1;
            }

            double dist = (tri1 - ray.Origin).Dot(normal) / div;
            if(dist < 0 || dist > 1)
            {
                return -1;
            }

            Vector3 posHitProj = ray.Origin + ray.Direction * dist;

            double uu = vec1Proj.Dot(vec1Proj);
            double uv = vec1Proj.Dot(vec2Proj);
            double vv = vec2Proj.Dot(vec2Proj);
            Vector3 w = posHitProj - tri1;
            double wu = w.Dot(vec1Proj);
            double wv = w.Dot(vec2Proj);

            double d = uv * uv - uu * vv;

            double s = (uv * wv - vv * wu) / d;
            if(s < 0 || s > 1.0)
            {
                return -1;
            }
            double t = (uv * wu - uu * wv) / d;
            if(t < 0 || s + t > 1.0)
            {
                return -1;
            }

            return dist;
        }

        private void GetTerrainMatches(RayData ray, RayTestHitFlags flags, List<RayResult> results)
        {
            List<ulong> pos = new List<ulong>();
            Vector3 regionExtents = new Vector3(m_Scene.SizeX, m_Scene.SizeY, 0);

            double d;
            for(d = 0; d < ray.RayLength; d += 0.5)
            {
                BoundingBox bbox = new BoundingBox();
                bbox.CenterOffset = ray.Origin + (ray.Direction * d);
                bbox.CenterOffset.X = Math.Floor(bbox.CenterOffset.X) + 0.5;
                bbox.CenterOffset.Y = Math.Floor(bbox.CenterOffset.Y) + 0.5;
                ulong tPos = (((ulong)bbox.CenterOffset.Y) << 32) + (ulong)bbox.CenterOffset.X;

                if (pos.Contains(tPos))
                {
                    /* skip if already tested */
                    continue;
                }
                pos.Add(tPos);

                if (bbox.CenterOffset.X < 0 || bbox.CenterOffset.X > regionExtents.X + 1 ||
                    bbox.CenterOffset.Y < 0 || bbox.CenterOffset.Y > regionExtents.Y + 1)
                {
                    /* skip */
                    continue;
                }

                Vector3 t0 = new Vector3(bbox.CenterOffset.X - 0.5, bbox.CenterOffset.Y - 0.5, 0);
                Vector3 t1 = new Vector3(bbox.CenterOffset.X + 0.5, bbox.CenterOffset.Y - 0.5, 0);
                Vector3 t2 = new Vector3(bbox.CenterOffset.X - 0.5, bbox.CenterOffset.Y + 0.5, 0);
                Vector3 t3 = new Vector3(bbox.CenterOffset.X + 0.5, bbox.CenterOffset.Y + 0.5, 0);
                t0.Z = m_Scene.Terrain[t0];
                t1.Z = m_Scene.Terrain[t1];
                t2.Z = m_Scene.Terrain[t2];
                t3.Z = m_Scene.Terrain[t3];

                Vector3 tmin = t0.ComponentMin(t1).ComponentMin(t2).ComponentMin(t3);
                Vector3 tmax = t0.ComponentMax(t1).ComponentMax(t2).ComponentMax(t3);
                bbox.CenterOffset.Z = (tmin.Z + tmax.Z) / 2;
                bbox.Size = tmax - tmin;

                double dist = IntersectBox(ray, ref bbox);
                if(dist < 0)
                {
                    /* not hitting terrain at all */
                    continue;
                }

                Vector3 normal;
                dist = IntersectTri(ray, t0, t1, t2, out normal);
                if (dist < 0)
                {
                    dist = IntersectTri(ray, t0, t3, t2, out normal);
                }

                if(dist < 0)
                {
                    continue;
                }

                RayResult res = new RayResult();
                res.HitNormalWorld = normal;
                res.HitPointWorld = ray.Origin + ray.Direction * dist;
                res.IsTerrain = true;
                res.ObjectId = UUID.Zero;
                res.PartId = UUID.Zero;
                results.Add(res);
            }
        }

        private void GetAgentMatches(RayData ray, RayTestHitFlags flags, List<RayResult> results)
        {
            foreach(IAgent agent in m_Scene.RootAgents)
            {
                BoundingBox bbox;
                agent.GetBoundingBox(out bbox);
                bbox.CenterOffset = agent.GlobalPosition;
                bbox.Size *= agent.GlobalRotation;
                bbox.Size = bbox.Size.ComponentMax(-bbox.Size);
                double dist = IntersectBox(ray, ref bbox);
                if(dist < 0)
                {
                    continue;
                }
                RayResult res = new RayResult();
                res.IsTerrain = false;
                res.ObjectId = agent.ID;
                res.PartId = agent.ID;
                res.HitPointWorld = ray.Origin + ray.Direction * dist;
                results.Add(res);
            }
        }

        private double IntersectBox(RayData ray, ref BoundingBox bbox)
        {
            double tmin;
            double tmax;
            double tymin;
            double tymax;
            double tzmin;
            double tzmax;

            tmin = (Math.Sign(ray.Direction.X) * bbox.Size.X + bbox.CenterOffset.X - ray.Origin.X) * ray.InvDirection.X;
            tmax = (-Math.Sign(ray.Direction.X) * bbox.Size.X + bbox.CenterOffset.X - ray.Origin.X) * ray.InvDirection.X;
            tymin = (Math.Sign(ray.Direction.Y) * bbox.Size.Y + bbox.CenterOffset.Y - ray.Origin.Y) * ray.InvDirection.Y;
            tymax = (-Math.Sign(ray.Direction.Y) * bbox.Size.Y + bbox.CenterOffset.Y - ray.Origin.Y) * ray.InvDirection.Y;
            
            if(tmin > tymax || tymin > tmax)
            {
                return -1;
            }
            
            if(tymin > tmin)
            {
                tmin = tymin;
            }
            if(tymax < tmax)
            {
                tmax = tymax;
            }

            tzmin = (Math.Sign(ray.Direction.Z) * bbox.Size.Z + bbox.CenterOffset.Z - ray.Origin.Z) * ray.InvDirection.Z;
            tzmax = (-Math.Sign(ray.Direction.Z) * bbox.Size.Z + bbox.CenterOffset.Z - ray.Origin.Z) * ray.InvDirection.Z;

            if(tmin > tzmax || tzmin > tmax)
            {
                return -1;
            }

            if(tzmin > tmin)
            {
                tmin = tzmin;
            }

            if(tzmax < tmax)
            {
                tmax = tzmax;
            }

            double t = tmin;
            
            if(t < 0)
            {
                t = tmax;
                if(t < 0)
                {
                    return -1;
                }
            }

            if(t > ray.RayLength)
            {
                return -1;
            }
            return t;
        }

        private readonly LLMesh.LodLevel[] LodOrder = new LLMesh.LodLevel[] { LLMesh.LodLevel.Physics, LLMesh.LodLevel.LOD0, LLMesh.LodLevel.LOD1, LLMesh.LodLevel.LOD2, LLMesh.LodLevel.LOD3 };

        private void GetObjectMatches(RayData ray, RayTestHitFlags flags, List<RayResult> results)
        { 
            foreach(ObjectGroup grp in m_Scene.ObjectGroups)
            {
                /* flag checks are cheap so do those first */
                if(((flags & RayTestHitFlags.NonPhantom) != 0 && !grp.IsPhantom) ||
                    ((flags & RayTestHitFlags.Phantom) != 0 && grp.IsPhantom) ||
                    ((flags & RayTestHitFlags.NonPhysical) != 0 && !grp.IsPhysics) ||
                    ((flags & RayTestHitFlags.Physical) != 0 && grp.IsPhysics))
                {
                    /* found a flag match */
                }
                else
                {
                    continue;
                }

                BoundingBox bbox;
                grp.GetBoundingBox(out bbox);
                bbox.CenterOffset = grp.GlobalPosition;
                bbox.Size *= grp.GlobalRotation;
                bbox.Size = bbox.Size.ComponentMax(-bbox.Size);
                double distance = IntersectBox(ray, ref bbox);
                if(distance < 0)
                {
                    /* only process if linkset bounding box is hit */
                    continue;
                }

                foreach(ObjectPart part in grp.ValuesByKey1)
                {
                    part.GetBoundingBox(out bbox);
                    distance = IntersectBox(ray, ref bbox);
                    if(distance < 0)
                    {
                        /* skip if not hit */
                        continue;
                    }

                    RayResult res = new RayResult();
                    res.ObjectId = grp.ID;
                    res.PartId = part.ID;

                    /* calculate actual HitPoint and HitNormal */
                    ObjectPart.PrimitiveShape shape = part.Shape;

                    MeshLOD lod = null;
                    if (shape.Type == PrimitiveShapeType.Sculpt && shape.SculptType == PrimitiveSculptType.Mesh)
                    {
                        var m = new LLMesh(m_Scene.AssetService[shape.SculptMap]);
                        foreach (LLMesh.LodLevel level in LodOrder)
                        {
                            if (m.HasLOD(level))
                            {
                                lod = m.GetLOD(level);
                                break;
                            }
                        }
                    }
                    else
                    {
                        lod = shape.ToMesh(m_Scene.AssetService);
                    }

                    if(lod != null)
                    {
                        lod.Optimize();
                        Vector3 normal;
                        foreach(Triangle tri in lod.Triangles)
                        {
                            double dist = IntersectTri(ray,
                                lod.Vertices[tri.Vertex1],
                                lod.Vertices[tri.Vertex2],
                                lod.Vertices[tri.Vertex3],
                                out normal);

                            if(dist >= 0)
                            {
                                res.HitNormalWorld = normal;
                                res.HitPointWorld = ray.Origin + ray.Direction * dist;
                                results.Add(res);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
