using System;
using UnityEngine;

namespace Synthesis.Configuration
{
    public class SelectableArrow : MonoBehaviour
    {
        private const float HiddenAlpha = 0.25f;

        private ArrowType arrowType;
        private Material material;
        private Color color;
        private bool selectable;

        private Quaternion startRotation;

        /// <summary>
        /// Initializes the <see cref="ArrowType"/> and saves the assigned
        /// <see cref="Material"/>.
        /// </summary>
        private void Start()
        {

            if (!Enum.TryParse(name, out arrowType))
                arrowType = ArrowType.None;

            material = GetComponent<Renderer>().material;
            color = material.color;
            selectable = true;
        }
        private void LateUpdate()
        {
            if (arrowType == ArrowType.P)//keeps marker looking at the camera
            {
                transform.LookAt(Camera.main.transform.position);
            }
            else if (arrowType <= ArrowType.Z)//keeps axis arrows looking at the camera
            {
                Vector3 difference = Camera.main.transform.position - transform.position;

                switch (arrowType)
                {
                    case ArrowType.X:
                        float rotationX = Mathf.Atan2(difference.z, difference.y) * Mathf.Rad2Deg;
                        transform.localRotation = Quaternion.Euler(rotationX + 90.0f, 0.0f, -90.0f);
                        break;
                    case ArrowType.Y:
                        float rotationY = Mathf.Atan2(difference.x, difference.z) * Mathf.Rad2Deg;
                        transform.localRotation = Quaternion.Euler(0, rotationY, 0);

                        break;
                    case ArrowType.Z:
                        float rotationZ = Mathf.Atan2(difference.x, difference.y) * Mathf.Rad2Deg;
                        transform.localRotation = Quaternion.Euler(rotationZ + 90.0f, 90.0f, 90.0f); //z axis rotation kind of messed up but this works
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Sends a message upwards when this <see cref="SelectableArrow"/>
        /// is selected.
        /// </summary>
        private void OnMouseDown()
        {
            SendMessageUpwards("OnArrowSelected", arrowType);
        }
        /// <summary>
        /// Sends a message upwards when this <see cref="SelectableArrow"/>
        /// is released.
        /// </summary>
        private void OnMouseUp()
        {
            SendMessageUpwards("OnArrowReleased");
            material.color = color;
        }

        /// <summary>
        /// Highlights the arrow yellow when it is hovered over.
        /// </summary>
        private void OnMouseEnter()
        {
            CameraController.isOverGizmo = true;
            if (selectable)
                material.color = Color.Lerp(color, new Color(30.0f / 255.0f, 164f / 255f, 212f / 255f, 1), 0.75f);
        }


        /// <summary>
        /// Returns the arrow to its original color when the mouse
        /// is no longer hovering over it.
        /// </summary>
        private void OnMouseExit()
        {
            CameraController.isOverGizmo = false;
            if (selectable)
                material.color = color;
        }

        /// <summary>
        /// Sets the alpha of this <see cref="SelectableArrow"/> according to the
        /// <see cref="ArrowType"/> provided.
        /// </summary>
        /// <param name="activeArrow"></param>
        private void SetActiveArrow(ArrowType activeArrow)
        {
            if (selectable = (activeArrow == ArrowType.None))
            {
                material.color = color;
            }
            else
            {
                Color newColor = material.color;
                newColor.a = arrowType == activeArrow ? 1f : color.a * HiddenAlpha;
                material.color = newColor;
            }
        }
    }
}