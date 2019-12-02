using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Narupa.Frontend.UI
{
    /// <summary>
    /// Script for arranging children radially around a point
    /// </summary>
    public class RadialLayoutGroup : LayoutGroup
    {
        [SerializeField]
        private GameObject radialPrefab;

        [SerializeField]
        private float angularOffset = 0;

        private void ArrangeChildrenRadially()
        {
            var children = Enumerable.Range(0, transform.childCount).Select(i => transform.GetChild(i)).Where(t => t.gameObject.activeSelf).ToList();
            var da = 360f / children.Count;
            for (var i = 0; i < children.Count; i++)
            {
                var angle = angularOffset + i * da;
                var d = Vector2.Scale(new Vector2(Mathf.Sin(Mathf.Deg2Rad * angle),
                                                  Mathf.Cos(Mathf.Deg2Rad * angle)),
                                      GetComponent<RectTransform>().sizeDelta * 0.5f);
                Debug.Log(GetComponent<RectTransform>().sizeDelta);
                children[i].transform.localPosition = d;
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
            ArrangeChildrenRadially();
        }
    }
}