using UnityEngine;

[ExecuteAlways]
public class AutoTilePlane : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Define the world unit size for a single texture tile.")]
    public float textureSize = 1.0f;
    
    public bool isURP = true; 

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private int _tilingPropertyId;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        _tilingPropertyId = Shader.PropertyToID(isURP ? "_BaseMap_ST" : "_MainTex_ST");
    }

    void Update()
    {
        if (_renderer == null) return;

        float tileX = transform.lossyScale.x / textureSize;
        float tileZ = transform.lossyScale.z / textureSize;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetVector(_tilingPropertyId, new Vector4(tileX, tileZ, 0, 0));
        _renderer.SetPropertyBlock(_propBlock);
    }
}