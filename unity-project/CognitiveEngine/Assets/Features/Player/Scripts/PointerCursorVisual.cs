using UnityEngine;

public class PointerCursorVisual : MonoBehaviour
{
    [SerializeField] private PointerTracker pointerTracker;
    [SerializeField] private float size = 0.3f;
    [SerializeField] private Color color = Color.blue;
    [SerializeField] private float zPosition = -1f;

    private GameObject visual;

    private void Start()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "PointerCursorVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(size, size, 1f);
        visual.transform.position = new Vector3(0f, 0f, zPosition);

        Collider col = visual.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = color;
            renderer.material = material;
        }
    }

    private void Update()
    {
        if (pointerTracker == null || visual == null)
            return;

        Vector2 pos = pointerTracker.GetPointerPosition();
        visual.transform.position = new Vector3(pos.x, pos.y, zPosition);
    }
}