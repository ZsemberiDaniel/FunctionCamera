using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EvaluateButton : MonoBehaviour {

    private Button button;

    public Distortable distortable;
    public FunctionInputField functionInput;
    
	void Start() {
        button = GetComponent<Button>();

        button.onClick.AddListener(() => {
            distortable.DistortCenterMiddle(functionInput.EvaluateCurrentInput());
        });
	}
}
