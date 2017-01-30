using UnityEngine;

public class LevelSwitcher : MonoBehaviour {
	void Update() {
		/*if(Input.GetKeyUp(KeyCode.J)) {
			Application.LoadLevel("intro");
		} else */if(Input.GetKeyUp(KeyCode.K)) {
			Application.LoadLevel("museum");
		} else if(Input.GetKeyUp(KeyCode.L)) {
			Application.LoadLevel("acropolis");
		}
	}
}
