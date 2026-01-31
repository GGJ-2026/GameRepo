using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private TextMeshProUGUI _text;
    private string _originalText;
    private bool _isSelected = false;

    void Awake()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (_text != null) _originalText = _text.text;
    }

    public void UpdateText(string newText)
    {
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        _originalText = newText;
        if (!_isSelected) _text.text = _originalText;
        else _text.text = "> " + _originalText;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetSelected(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetSelected(false);
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        SetSelected(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
    }

    private void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_text == null) return;
        
        string clean = _text.text.Replace("> ", ""); 
        
        if (selected)
        {
            _text.text = "> " + clean;
        }
        else
        {
            _text.text = clean;
        }
    }
}