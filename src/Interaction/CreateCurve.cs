using OpenTK;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreateCurve : Interaction
    {
        public enum State
        {
            READY = 0, PAINT = 1
        };

        protected State currentState;
        protected Geometry.Geometry stroke_g;
        protected Material.Material stroke_m;
        protected uint primaryDeviceIndex;
        protected Guid strokeId;
        protected List<Vector3> reducePoints = new List<Vector3>();

        // Pops this interaction of the stack after releasing stroke if true.
        bool mPopAfterStroke = false;

        //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets
        public int type = 0;
        public bool isClosed = false;
        public List<Guid> ListTargets = new List<Guid>(); //could be added in init() pass argument

        private bool hitPlane = false;
        private bool lockPlane = false;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        protected SceneNode drawPoint;
        protected OpenTK.Vector3 projectP;

        //testing rhino curve
        private List<Point3d> rhinoCurvePoints = new List<Point3d>();
        private Rhino.Geometry.Curve rhinoCurve;
        private Plane proj_plane;
        private List<Point3d> simplifiedCurvePoints = new List<Point3d>();
        private Rhino.Geometry.NurbsCurve simplifiedCurve;
        protected RhinoObject curveOnObj;


        public CreateCurve(ref Scene scene) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
        }
        public CreateCurve(ref Scene scene, int _type, bool _isClosed) : base(ref scene)
        {
            mScene = scene;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
            type = _type;
            isClosed = _isClosed;

            if (type != 0)
            {

                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
                /*
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(1, 1, 1, 1);
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);*/

            }

        }

        public CreateCurve(ref Scene scene, uint devIndex) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref scene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
            primaryDeviceIndex = devIndex;
        }

        public override void init()
        {
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
            }
        }

        public override void draw(bool isTop)
        {

            //visualize the point on the plane for type = 1, 2, 3
            if (type != 0 && isTop)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                //settings.NameFilter = "plane";
                float mimD = 1000000f;
                hitPlane = false;
                //lock the active plane when users start drawing
                if (!lockPlane)
                {
                    foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                    {
                        //check for different drawing curve types
                        bool b1 = (type == 1) && rhObj.Attributes.Name.Contains("plane");
                        bool b2 = (type == 2) && (rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint"));
                        bool b3 = (type == 3) && ListTargets.Contains(rhObj.Id);

                        //only drawing on planes for now rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("plane")
                        //if (rhObj.Attributes.Name.Contains("plane"))
                        if (b1 || b2 || b3)
                        {
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            geometries.Add(rhObj.Geometry);
                            //must be a brep or surface, not mesh
                            Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                            if (rayIntersections != null)
                            {
                                //get the nearest one
                                OpenTK.Vector3 tmpP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                                float distance = (float)Math.Sqrt(Math.Pow(tmpP.X - controller_p.X, 2) + Math.Pow(tmpP.Y - controller_p.Y, 2) + Math.Pow(tmpP.Z - controller_p.Z, 2));

                                if (distance < mimD)
                                {
                                    hitPlane = true;
                                    targetPSN = mScene.brepToSceneNodeDic[rhObj.Id];
                                    targetPRhObj = rhObj;
                                    mimD = distance;
                                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (targetPRhObj != null)
                    {
                        List<GeometryBase> geometries = new List<GeometryBase>();
                        geometries.Add(targetPRhObj.Geometry);
                        //must be a brep or surface, not mesh
                        Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                        if (rayIntersections != null)
                        {
                            //get the nearest one
                            OpenTK.Vector3 tmpP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                            float distance = (float)Math.Sqrt(Math.Pow(tmpP.X - controller_p.X, 2) + Math.Pow(tmpP.Y - controller_p.Y, 2) + Math.Pow(tmpP.Z - controller_p.Z, 2));

                            if (distance < mimD)
                            {
                                hitPlane = true;
                                mimD = distance;
                                projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                            }
                        }
                    }
                }

                if (!hitPlane)
                {
                    targetPSN = null;
                    targetPRhObj = null;
                    projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable
                }

                //visualize the projection points
                // inverted rotation first

                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                t.Transpose();
                drawPoint.transform = t;
            }

            if (currentState != State.PAINT || !isTop)
            {
                return;
            }

            // drawing curve
            Vector3 pos = new Vector3();
            if (type != 0)
            {
                pos = projectP;
                if (hitPlane)
                {
                    //GeometryStroke handle rotation
                    ((Geometry.GeometryStroke)stroke_g).addPoint(pos);
                    rhinoCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));
                    //store the targeObj
                    curveOnObj = targetPRhObj;
                }

            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx));
                //GeometryStroke handle rotation already
                ((Geometry.GeometryStroke)stroke_g).addPoint(pos);
                rhinoCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));
            }

            if (((Geometry.GeometryStroke)stroke_g).mNumPrimitives == 1)
            {
                SceneNode stroke = new SceneNode("Stroke", ref stroke_g, ref stroke_m);
                mScene.tableGeometry.add(ref stroke);
                strokeId = stroke.guid;
            }

            //testing the performance of rhino curve and might be used for dynamically rendering
            if (rhinoCurvePoints.Count == 2)
            {
                rhinoCurve = Rhino.Geometry.Curve.CreateInterpolatedCurve(rhinoCurvePoints.ToArray(), 3);
            }
            else if (rhinoCurvePoints.Count > 2)
            {
                rhinoCurve.Extend(Rhino.Geometry.CurveEnd.End, Rhino.Geometry.CurveExtensionStyle.Line, rhinoCurvePoints[rhinoCurvePoints.Count - 1]);
            }

        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip click event test");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {
                lockPlane = true;
                stroke_g = new Geometry.GeometryStroke(ref mScene);
                reducePoints = new List<Vector3>();
                currentState = State.PAINT;
            }

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {
                lockPlane = false;

                //simplfy the curve first before doing next interaction
                if (((Geometry.GeometryStroke)(stroke_g)).mPoints.Count >= 2)
                {
                    simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

                    //intialize the rhino points of curve
                    foreach (OpenTK.Vector3 point in reducePoints)
                    {
                        simplifiedCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
                        //Util.MarkPoint(ref mScene.staticGeometry, new Vector3(point.X, point.Y, point.Z), 1, 1, 1);
                    }

                    if (rhinoCurvePoints.Count >= 2) //TODO: might need 8 for closecurve check
                    {
                        //clear the stroke and the drawPoint
                        foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                        {
                            if (sn.guid == strokeId)
                            {
                                mScene.tableGeometry.children.Remove(sn);

                            }
                            else if (sn.name == "drawPoint")
                            {
                                mScene.tableGeometry.children.Remove(sn);

                            }
                        }
                        //mScene.tableGeometry.children.RemoveAll(item => item.guid == strokeId);
                        //mScene.tableGeometry.children.RemoveAll(item => item.name == "drawPoint");

                        if (isClosed)
                        {
                            simplifiedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, simplifiedCurvePoints.ToArray());
                        }
                        else
                        {
                            simplifiedCurve = Rhino.Geometry.NurbsCurve.Create(false, 3, simplifiedCurvePoints.ToArray());

                        }

                        //add to Scene curve object ,targetRhobj and check the next interaction
                        mScene.iCurveList.Add(simplifiedCurve);
                        if (type != 0 && curveOnObj != null)
                        {
                            mScene.iRhObjList.Add(curveOnObj);
                        }

                        //update the interaction chain
                        /*
                        mScene.iIndex++;

                        if (mScene.iChain.ElementAt(mScene.iIndex) == "EditPointOnP")
                        {
                            mScene.popInteraction();
                            mScene.pushInteraction(new EditPoint2(ref mScene, true));
                        }*/
                        mScene.popInteraction();
                        mScene.peekInteraction().init();



                    }

                    currentState = State.READY;
                    curveOnObj = null;

                }
            }
        }


        public void simplifyCurve(ref List<Vector3> curvePoints)
        {
            float pointReductionTubeWidth = 0.002f;
            reducePoints = DouglasPeucker(ref curvePoints, 0, curvePoints.Count - 1, pointReductionTubeWidth);
            Rhino.RhinoApp.WriteLine("reduce points from" + curvePoints.Count + " to " + curvePoints.Count);
        }

        //Quick test about Douglas-Peucker for rhino points, return point3d with rhino coordinate system
        public List<Vector3> DouglasPeucker(ref List<Vector3> points, int startIndex, int lastIndex, float epsilon)
        {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i)
            {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon)
            {
                List<Vector3> res1 = DouglasPeucker(ref points, startIndex, index, epsilon);
                List<Vector3> res2 = DouglasPeucker(ref points, index, lastIndex, epsilon);

                //watch out the coordinate system
                List<Vector3> finalRes = new List<Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i)
                {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i)
                {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else
            {
                return new List<Vector3>(new Vector3[] { points[startIndex], points[lastIndex] });
            }
        }

        public float PointLineDistance(Vector3 point, Vector3 start, Vector3 end)
        {

            if (start == end)
            {
                return (float)Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2) + Math.Pow(point.Z - start.Z, 2));
            }

            Vector3 u = new Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            Vector3 pq = new Vector3(point.X - start.X, point.Y - start.Y, point.Z - start.Z);

            return Vector3.Cross(pq, u).Length / u.Length;


        }

    }
}