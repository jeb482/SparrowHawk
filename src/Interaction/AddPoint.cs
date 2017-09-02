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
    class AddPoint : Interaction
    {
        //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets
        public int type = 0;
        public int maxNumPoint = 100;
        public List<Guid> ListTargets = new List<Guid>();
        protected Geometry.Geometry point_g;
        protected Material.Material point_m;

        private bool hitPlane = false;
        private bool lockPlane = false;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        protected SceneNode drawPoint;
        protected OpenTK.Vector3 projectP;
        Vector3 pos = new Vector3();
        protected RhinoObject pointOnObj;
        private string renderType = "none";
        private List<Point3d> pointsList = new List<Point3d>();

        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Material.Material profile_m;

        public AddPoint(ref Scene scene) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
        }

        public AddPoint(ref Scene scene, int _type) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
            }
        }

        public AddPoint(ref Scene scene, int _type, int num) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            maxNumPoint = num;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
            }
        }

        public AddPoint(ref Scene scene, int _type, int num, string rtype) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            renderType = rtype;
            profile_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            maxNumPoint = num;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
            }
        }

        public override void init()
        {
            if ((type == 3) && mScene.iRhObjList.Count != 0)
            {
                foreach (Rhino.DocObjects.RhinoObject RhObj in mScene.iRhObjList)
                {
                    ListTargets.Add(RhObj.Id);
                }

            }
        }

        public override void draw(bool isTop)
        {
            //visualize the point on the plane for type = 1, 2, 3
            if (type != 0 && isTop)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
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
                        bool b2 = (type == 2) && (rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("patchSurface"));
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

                pointOnObj = targetPRhObj;

                //visualize the projection points
                // inverted rotation first

                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                t.Transpose();
                drawPoint.transform = t;

                pos = projectP;
            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx));
            }
        }

        public void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    if (sn.name == "drawPoint" || sn.name == "EditPoint")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                    }
                }
            }

            pointsList.Clear();
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            //testing
            mScene.iPointList.Add(pos); //not using iPointList anymore but added here since EditPoint3 still use
            pointsList.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));

            //TODO- figure out why here need mScene.tableGeometry.transform.Inverted() but others don't
            OpenTK.Vector3 p = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), pos);
            SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, p, 0, 1, 0);
            pointMarkers.Add(sn);

            //mScene.iPointList.Add(p);

            if (maxNumPoint == pointMarkers.Count)
            {
                //detecting plane
                Brep targetBrep = (Brep)(pointOnObj.Geometry);
                //compute the brepFace where the curve is on
                //Surface s = targetBrep.Faces[0];

                //TODO- topLeftP won't be on the face in the 3D case. so probably use orgin
                int faceIndex = -1;
                for (int i = 0; i < targetBrep.Faces.Count; i++)
                {
                    //cast BrepFace to Brep for ClosestPoint(P) menthod
                    double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(pointsList[0]).DistanceTo(pointsList[0]);
                    //debuging mScene.rhinoDoc.ModelAbsoluteTolerance                   
                    if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                    {
                        faceIndex = i;
                        break;
                    }
                }
                Surface s = targetBrep.Faces[faceIndex];
                Plane curvePlane = new Plane() ;
                NurbsCurve modelcurve = null;
                Brep modelBrep;

                //surface might not be a perfect planar surface
                Double tolerance = 0;
                while (tolerance < 100)
                {
                    if (s.TryGetPlane(out curvePlane, tolerance))
                    {
                        break;
                    }
                    tolerance++;
                }

                //tolerance < 100 == finding a cvurve plane
                if (tolerance < 100)
                {
                    if (renderType == "Circle")
                    {
                        float radius = (float)Math.Sqrt(Math.Pow(pointsList[1].X - pointsList[0].X, 2) + Math.Pow(pointsList[1].Y - pointsList[0].Y, 2) + Math.Pow(pointsList[1].Z - pointsList[0].Z, 2));
                        Circle circle = new Rhino.Geometry.Circle(curvePlane, pointsList[0], radius);
                        modelcurve = circle.ToNurbsCurve();

                    }
                    else if (renderType == "Rect")
                    {
                        /*
                        Vector3 rectDiagonalV = new Vector3((float)(pointsList[0].X - pointsList[1].X), (float)(pointsList[0].Y - pointsList[1].Y), (float)(pointsList[0].Z - pointsList[1].Z));
                        float lenDiagonal = rectDiagonalV.Length;
                        Vector3 rectLeftTop = new Vector3((float)pointsList[0].X, (float)pointsList[0].Y, (float)pointsList[0].Z) + lenDiagonal * rectDiagonalV.Normalized();
                        Point3d topLeftP = new Point3d(rectLeftTop.X, rectLeftTop.Y, rectLeftTop.Z);
                        
                        */

                        //using top-left cornel and bottom right
                        
                        Rectangle3d rect = new Rectangle3d(curvePlane, pointsList[0], pointsList[1]);

                        modelcurve = rect.ToNurbsCurve();
                        
                    }

                    Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                    modelBrep = shapes[0];
                    Guid renderObjId = Util.addSceneNode(ref mScene, modelBrep, ref profile_m, renderType);
                    //add icurveList since we don't use EditPoint2 for circle and rect
                    mScene.iCurveList.Add(modelcurve);
                    //mScene.iPlaneList.Add(ref curvePlane);
                }


                if (type != 0 && pointOnObj != null)
                {
                    mScene.iRhObjList.Add(pointOnObj);
                }

                clearDrawing();
                mScene.popInteraction();
                if (!mScene.interactionStackEmpty())
                    mScene.peekInteraction().init();
            }
        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {

        }
    }
}
