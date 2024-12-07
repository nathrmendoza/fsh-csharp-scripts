using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class OutlineShaderController : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Range(0, 4)] private float outlineWidth = 2f;

    private Material material;
    private bool isOutlineVisible;

    private void Awake()
    {
        // Get the renderer and its material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        // Create a new material instance using our shader
        material = new Material(Shader.Find("Custom/InteractableOutline"));
        material.SetColor("_OutlineColor", outlineColor);
        material.SetFloat("_OutlineWidth", outlineWidth);
        material.SetFloat("_EnableOutline", 0); // Start with outline disabled

        // Assign the material
        meshRenderer.material = material;
    }

    public void ShowOutline()
    {
        if (!isOutlineVisible)
        {
            material.SetFloat("_EnableOutline", 1);
            isOutlineVisible = true;
        }
    }

    public void HideOutline()
    {
        if (isOutlineVisible)
        {
            material.SetFloat("_EnableOutline", 0);
            isOutlineVisible = false;
        }
    }

    public void SetMainColor(Color color)
    {
        material.SetColor("_Color", color);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
