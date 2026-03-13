using TMPro;
using UnityEngine;

public class AdminConsoleCategoryHeader : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    public void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }
}
