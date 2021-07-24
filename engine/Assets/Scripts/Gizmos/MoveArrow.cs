using System.Collections.Generic;
using UnityEngine;


namespace Synthesis.Configuration
{
    public class MoveArrow : MonoBehaviour
    {
        private const float Scale = 0.075f;
        private Vector3 initialScale;
        private Vector3 lastArrowPoint;
        private ArrowType activeArrow;
        private bool bufferPassed;
        private bool snapEnabled;

        private Plane axisPlane;
        private Plane markerPlane;

        private Transform parent;
        private Transform originalCameraTransform;
        private Transform gizmoCameraTransform;
        Dictionary<Rigidbody, bool> rigidbodiesKinematicStateInScene;

        private CameraController cam;
        private float originalLowerPitch;
        private float gizmoPitch = -80f;

        [SerializeField, Range(1f, 30.0f)] public float snapRotationToDegree; //configurable
        [SerializeField, Range(0.1f, 2f)] public float snapTransformToUnit;
        private float floorBound = 0f;
        private float bounds = 50f;
        private float singleMoveLimitScale = 20f; //limits a single movement boundry to this number times the distance from camera to position

        private Transform arrowX;
        private Transform arrowY;
        private Transform arrowZ;

        //Press R to reset rotation
        //Press CTRL to snap to nearest configured multiple when moving
        //Added gameObjects to Game while this script is active will not have their rigidbodies disabled


        /// <summary>
        /// Gets or sets the active selected arrow. When <see cref="ActiveArrow"/>
        /// is changed, the "SetActiveArrow" message is broadcasted to all
        /// <see cref="SelectableArrow"/>s.
        /// </summary>
        private ArrowType ActiveArrow
        {
            get
            {
                return activeArrow;
            }
            set
            {
                activeArrow = value;
                BroadcastMessage("SetActiveArrow", activeArrow);
            }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> representing the direction the selected
        /// arrow is facing, or <see cref="Vector3.zero"/> if no arrow is selected.
        /// </summary>
        private Vector3 ArrowDirection
        {
            get
            {
                switch (ActiveArrow)
                {
                    case ArrowType.X:
                    case ArrowType.YZ:
                    case ArrowType.RX:
                        return transform.right;
                    case ArrowType.Y:
                    case ArrowType.XZ:
                    case ArrowType.RY:
                        return transform.up;
                    case ArrowType.Z:
                    case ArrowType.XY:
                    case ArrowType.RZ:
                        return transform.forward;
                    default:
                        return Vector3.zero;
                }
            }
        }


        /// <summary>
        /// Sets the initial position and rotation.
        /// </summary>
        private void Awake()
        {
            cam = Camera.main.GetComponent<CameraController>();
            originalLowerPitch = cam.PitchLowerLimit;
            originalCameraTransform = cam.FollowTransform;



            HierarchyRigidbodiesToDictionary();
            setTransform();

            arrowX = transform.Find("X").GetComponent<Transform>();
            arrowY = transform.Find("Y").GetComponent<Transform>();
            arrowZ = transform.Find("Z").GetComponent<Transform>();

            initialScale = new Vector3(transform.localScale.x / transform.lossyScale.x,
                transform.localScale.y / transform.lossyScale.y, transform.localScale.z / transform.lossyScale.z);
        }
        private void setTransform()
        {
            SetOtherCollidersEnabled(false);

            parent = transform.parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            gizmoCameraTransform = new GameObject().transform;
            gizmoCameraTransform.position = transform.parent.position;
            cam.FollowTransform = gizmoCameraTransform;
            cam.PitchLowerLimit = gizmoPitch;
        }
        private void disableGizmo()
        {
            cam.PitchLowerLimit = originalLowerPitch;
            cam.FollowTransform = originalCameraTransform;
            SetOtherCollidersEnabled(true);
        }

        private void OnTransformParentChanged()//only called for testing. 
        {
            if (transform.parent != null)
            {
                setTransform();
            }
        }
        /// <summary>
        /// Disables all colliders of any parent objects to allow for proper click detection.
        /// </summary>
        private void OnEnable()
        {
            setTransform();
        }

        /// <summary>
        /// Re-enables all colliders of any parent objects to allow for their own click detection.
        /// </summary>
        private void OnDisable()
        {
            disableGizmo();
        }
        private void OnDestroy()
        {
            disableGizmo();
        }

        
        /// <summary>
        /// Updates the robot's position when the arrows are dragged.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))//Reset
            {
                parent.rotation = Quaternion.identity;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                snapEnabled = true;
            }
            else
            {
                snapEnabled = false;
            }
            if (activeArrow == ArrowType.None)
                return;

            // This allows for any updates from OnClick to complete before translation starts
            if (!bufferPassed)
            {
                bufferPassed = true;
                return;
            }

            if (activeArrow == ArrowType.P)
            {
                Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                float rayLength;
                float rayLimit = Vector3.Distance(Camera.main.transform.position, gizmoCameraTransform.position) * singleMoveLimitScale;

                //creates boundries for moving planes; also: if ray doesn't intersect the plane, 
                if (!markerPlane.Raycast(cameraRay, out rayLength))
                    rayLength = rayLimit;
                parent.position = markerPlane.ClosestPointOnPlane(cameraRay.GetPoint(rayLength));
                if (parent.position.y < 0) parent.position = new Vector3(parent.position.x, 0, parent.position.z);
            }
            else if (activeArrow <= ArrowType.XY)
            {

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 currentArrowPoint;

                if (activeArrow <= ArrowType.Z)
                {

                    Transform arrow;
                    switch (activeArrow)
                    {
                        case ArrowType.X:
                            arrow = arrowX;
                            break;
                        case ArrowType.Y:
                            arrow = arrowY;
                            break;
                        case ArrowType.Z:
                            arrow = arrowZ;
                            break;
                        default:
                            arrow = transform;
                                break;                    
                    }
                    Plane axisArrowPlane = new Plane(arrow.forward, parent.position);


                    float rayDistance;
                    float rayLimit = Vector3.Distance(Camera.main.transform.position, gizmoCameraTransform.position) * singleMoveLimitScale;

                    if (!axisArrowPlane.Raycast(mouseRay, out rayDistance) || rayDistance > rayLimit)
                        rayDistance = rayLimit;

                    ClosestPointsOnTwoLines(out Vector3 p, out currentArrowPoint,axisArrowPlane.ClosestPointOnPlane(mouseRay.GetPoint(rayDistance)), arrow.right,parent.position,ArrowDirection);

                    /*
                    Vector3 closestPointScreenRay;

                    ClosestPointsOnTwoLines(out closestPointScreenRay, out currentArrowPoint,
                     mouseRay.origin, mouseRay.direction, parent.position, ArrowDirection);
                    Debug.DrawLine(closestPointScreenRay, currentArrowPoint);*/

                }
                else
                {
                    Plane plane = new Plane(ArrowDirection, parent.position);

                    float rayDistance;
                    float rayLimit = Vector3.Distance(Camera.main.transform.position, gizmoCameraTransform.position) * singleMoveLimitScale;

                    if (!plane.Raycast(mouseRay, out rayDistance) || rayDistance > rayLimit) 
                        rayDistance = rayLimit;

                    currentArrowPoint = plane.ClosestPointOnPlane(mouseRay.GetPoint(rayDistance));
                }
                bool setLastArrowPoint = true;
                if (lastArrowPoint != Vector3.zero)
                {
                    if (snapEnabled && activeArrow <= ArrowType.Z)//snaps to configurable amount when control is held down. does this by settings current arrow point to rounded distance
                        currentArrowPoint = LerpByDistance(lastArrowPoint, currentArrowPoint,
                            RoundTo(Vector3.Distance(lastArrowPoint, currentArrowPoint), snapTransformToUnit));

                    Vector3 projectedPosition = parent.position + currentArrowPoint - lastArrowPoint;
                    setLastArrowPoint = projectedPosition.y >= floorBound;//sets movement boundries
                    if (setLastArrowPoint)
                        parent.position += currentArrowPoint - lastArrowPoint;
                }
                if(setLastArrowPoint)  lastArrowPoint = currentArrowPoint;

            }
            else
            {
                //Project a ray from mouse
                Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                float rayLength;

                //if mouse ray doesn't intersect plane, set default ray length
                if (!axisPlane.Raycast(cameraRay, out rayLength)) rayLength = Vector3.Distance(Camera.main.transform.position, parent.position) * 10;

                //get intersection point; if none, find closest point to default length
                Vector3 pointToLook = axisPlane.ClosestPointOnPlane(cameraRay.GetPoint(rayLength));

                //Correct parent's forward depending on rotation axis. Y-axis does not need corrections
                Vector3 t;
                if (ActiveArrow == ArrowType.RZ) t = parent.right;
                else if (ActiveArrow == ArrowType.RX) t = parent.up;
                else t = parent.forward;
                parent.RotateAround(parent.position, axisPlane.normal, //defines point and axis plane
                    -1 * RoundTo(Vector3.SignedAngle(pointToLook - parent.position, t, axisPlane.normal), //rounds degrees of rotation axis forward to mouse ray intersection
                    snapEnabled ? snapRotationToDegree : 0f)); //if control is pressed, snap to configurable value, otherwise, don't snap
            }

            transform.parent = parent;

        }

        /// <summary>
        /// Scales the arrows to maintain a constant size relative to screen coordinates.
        /// </summary>
        private void LateUpdate()
        {
            Plane plane = new Plane(Camera.main.transform.forward, Camera.main.transform.position);
            float dist = plane.GetDistanceToPoint(transform.position);
            transform.localScale = initialScale * Scale * dist;
            Vector3 scaleTmp = gameObject.transform.localScale;
            scaleTmp.x /= parent.localScale.x;
            scaleTmp.y /= parent.localScale.y;
            scaleTmp.z /= parent.localScale.z;
            gameObject.transform.localScale = scaleTmp;

        }
        void DrawPlane(Vector3 normal, Vector3 position)//for debug only, can be removed
        {

            Vector3 v3;

            if (normal.normalized != Vector3.forward)
                v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude * 2;
            else
                v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude * 2;

            var corner0 = position + v3;
            var corner2 = position - v3;
            var q = Quaternion.AngleAxis(90.0f, normal);
            v3 = q * v3;
            var corner1 = position + v3;
            var corner3 = position - v3;

            Debug.DrawLine(corner0, corner2, Color.green);
            Debug.DrawLine(corner1, corner3, Color.green);
            Debug.DrawLine(corner0, corner1, Color.green);
            Debug.DrawLine(corner1, corner2, Color.green);
            Debug.DrawLine(corner2, corner3, Color.green);
            Debug.DrawLine(corner3, corner0, Color.green);
            Debug.DrawRay(position, normal, Color.red);
        }

        /// <summary>
        /// Sets the active arrow when a <see cref="SelectableArrow"/> is selected.
        /// </summary>
        /// <param name="arrowType"></param>
        private void OnArrowSelected(ArrowType arrowType)
        {
            ActiveArrow = arrowType;
            lastArrowPoint = Vector3.zero;
            bufferPassed = false;

            if (arrowType == ArrowType.P)
                markerPlane = new Plane(Vector3.Normalize(Camera.main.transform.forward), parent.position);
            else if (arrowType >= ArrowType.RX)
                axisPlane = new Plane(ArrowDirection, parent.position);

        }

        /// <summary>
        /// Sets the active arrow to <see cref="ArrowType.None"/> when a
        /// <see cref="SelectableArrow"/> is released.
        /// </summary>
        private void OnArrowReleased()
        {
            ActiveArrow = ArrowType.None;

            //detect if object is out of bounds
            float x = transform.parent.position.x;
            float y = transform.parent.position.y;
            float z = transform.parent.position.z;
            transform.parent.position = new Vector3(
                Mathf.Abs(x)>bounds ? (x/Mathf.Abs(x)*bounds) : x,
                Mathf.Abs(y)>bounds ? (y/Mathf.Abs(y)*bounds) : y,
                Mathf.Abs(z)>bounds ? (z/Mathf.Abs(z)*bounds) : z);

            //move the camera
            gizmoCameraTransform.position = transform.parent.position;
            cam.FollowTransform = gizmoCameraTransform;
            
        }
        private void HierarchyRigidbodiesToDictionary() //save the state of all gameobject's rigidbodies as a dictionary
        {
            rigidbodiesKinematicStateInScene = new Dictionary<Rigidbody, bool>();
            GameObject Game = GameObject.Find("Game");
            foreach(Rigidbody rb in Game.GetComponentsInChildren<Rigidbody>())
            {
                if(rb.gameObject.transform.parent != transform)//skips gizmos
                rigidbodiesKinematicStateInScene.Add(rb, rb.isKinematic);
            }
        }
        /// <summary>
        /// Enables or disables other colliders to ensure proper arrow click
        /// detection.
        /// </summary>
        /// <param name="enabled"></param>
        private void SetOtherCollidersEnabled(bool enabled)//CLEAN THIS UP
        {
            foreach(KeyValuePair<Rigidbody,bool> rb in rigidbodiesKinematicStateInScene)
            {
                if (enabled)
                {
                    rb.Key.isKinematic = rb.Value;
                    rb.Key.detectCollisions = true;
                }
                else
                {
                    rb.Key.isKinematic = true;
                    rb.Key.detectCollisions = false;
                }
            }
        }


        /// <summary>
        /// Based on a solution provided by the Unity Wiki (http://wiki.unity3d.com/index.php/3d_Math_functions).
        /// Finds the closest points on two lines.
        /// </summary>
        /// <param name="closestPointLine1"></param>
        /// <param name="closestPointLine2"></param>
        /// <param name="linePoint1"></param>
        /// <param name="lineVec1"></param>
        /// <param name="linePoint2"></param>
        /// <param name="lineVec2"></param>
        /// <returns></returns>
        private bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            closestPointLine1 = Vector3.zero;
            closestPointLine2 = Vector3.zero;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            // Check if lines are parallel
            if (d == 0.0f)
                return false;

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }
        float RoundTo(float value, float multipleOf)//used for snapping the gizmo to the nearest value
        {
            if (multipleOf != 0)
                return Mathf.Round(value / multipleOf) * multipleOf;
            else
                return value;
        }
        /// <summary>
        /// Finds the Vector3 point a distance of x away from Point A and on line AB
        /// </summary>
        public Vector3 LerpByDistance(Vector3 A, Vector3 B, float x) // for snapping transformations
        {
            Vector3 P = x * Vector3.Normalize(B - A) + A;
            return P;
        }


    }
}