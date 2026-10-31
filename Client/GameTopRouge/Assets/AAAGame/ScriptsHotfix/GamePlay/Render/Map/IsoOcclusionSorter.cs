using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>
    /// Caches an environment sprite's order from its ground contact point.
    /// The role renderer recalculates against this same rule while moving.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class IsoOcclusionSorter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Transform groundAnchor;
        [SerializeField] private int sameDepthOrderOffset = 1;

        private void OnEnable()
        {
            RefreshSortingOrder();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                RefreshSortingOrder();
            }
        }

        public void RefreshSortingOrder()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetRenderer == null)
            {
                return;
            }

            float footY = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
            targetRenderer.sortingOrder = MapSorting.WorldObjectOrder(footY) + sameDepthOrderOffset;
        }
    }
}