using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Spotlight
{
    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private InteractionController interactionController;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private LayerMask interactableMask;
        [SerializeField] private LayerMask walkableMask;
        [SerializeField] private bool inputEnabled = true;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public event Action<InputRouteResult> RouteResolved;
        public bool InputEnabled => inputEnabled;
        public InputRouteResult LastRoute { get; private set; } = InputRouteResult.Ignored;

        public void Configure(Camera camera, InteractionController interactions,
            PlayerMovement movement, LayerMask interactionLayers, LayerMask walkableLayers)
        {
            worldCamera = camera;
            interactionController = interactions;
            playerMovement = movement;
            interactableMask = interactionLayers;
            walkableMask = walkableLayers;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RoutePointerDown(Input.mousePosition);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        public InputRouteResult RoutePointerDown(Vector2 screenPosition)
        {
            if (!inputEnabled)
            {
                return Resolve(InputRouteResult.Disabled);
            }

            if (IsPointerOverUI(screenPosition))
            {
                return Resolve(InputRouteResult.UI);
            }

            worldCamera ??= Camera.main;
            if (worldCamera == null)
            {
                return Resolve(InputRouteResult.Ignored);
            }

            float depth = Mathf.Abs(worldCamera.transform.position.z);
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            Vector2 worldPoint = new Vector2(world.x, world.y);

            Collider2D interactionTarget = Physics2D.OverlapPoint(worldPoint, interactableMask);
            if (interactionTarget != null)
            {
                interactionController?.TryInteract(interactionTarget);
                return Resolve(InputRouteResult.Interaction);
            }

            Collider2D walkableTarget = Physics2D.OverlapPoint(worldPoint, walkableMask);
            if (walkableTarget != null)
            {
                playerMovement?.SetTargetX(world.x);
                return Resolve(InputRouteResult.Movement);
            }

            return Resolve(InputRouteResult.Ignored);
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            uiHits.Clear();
            PointerEventData eventData = new PointerEventData(eventSystem) { position = screenPosition };
            eventSystem.RaycastAll(eventData, uiHits);
            return uiHits.Count > 0;
        }

        private InputRouteResult Resolve(InputRouteResult result)
        {
            LastRoute = result;
            RouteResolved?.Invoke(result);
            return result;
        }
    }
}
