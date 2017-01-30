using UnityEngine;
using UnityEngine.Assertions;

public class ItemData : MonoBehaviour {
	public int id = -1; /* default: no matching CombinableElement */
	public Texture2D icon;
	public string item_name;
	public string description;

	[HideInInspector]
	public GameObject this_game_object;

	void Start() {
		this_game_object = this.gameObject;

		Assert.IsNotNull(this_game_object);
		Assert.IsTrue(this_game_object.tag == "Item");	
	}
}
