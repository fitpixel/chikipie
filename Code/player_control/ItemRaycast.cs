using UnityEngine;
using UnityEngine.Assertions;
using UnityStandardAssets.Characters.FirstPerson;

public class ItemRaycast : MonoBehaviour {
	public Camera cam;
	public float depth = 100;

	Fieldbook fieldbook;

	bool display = false;
	ItemData item_data;
	Rect item_name_rect, item_description_rect;
	GUIStyle centered_style;

	void Start() {
		fieldbook = Fieldbook.Instance;

		Assert.IsNotNull(cam);
		Assert.IsNotNull(fieldbook);

		centered_style = new GUIStyle("label");
		centered_style.alignment = TextAnchor.MiddleCenter;

		item_name_rect = new Rect(
            Screen.width * 0.1F,
			0.0F,
			Screen.width * 0.9F,
			Screen.height * 1.0F
		);
		item_description_rect = new Rect(
			Screen.width * 0.1F,
			Screen.height * 0.05F + item_name_rect.y,
			Screen.width * 0.9F,
			Screen.height * 0.95F
		);
	}

	void Update() {
		/* don't raycast if the inventory is also being displayed */
		if(fieldbook.display) {
			display = false;
			return;
		}

		/* you can only add something to your inventory if you're in first person mode */
		if(fieldbook.display) return;

		RaycastHit hit;
		Renderer item_renderer = null;
		if(Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward), out hit) /* if you hit an object */ &&
		   (item_renderer = hit.transform.gameObject.GetComponent<Renderer>()) != null &&
		   item_renderer.enabled && hit.transform.gameObject.tag == "Item" /* and that object was visible and is an item */) {
			if(!display || item_data != hit.transform.gameObject.GetComponent<ItemData>()) { /* only update item_data if we aren't displaying anything or it's a different item */
				item_data = hit.transform.gameObject.GetComponent<ItemData>();
				Assert.IsNotNull(item_data);
			}
			display = true;
		} else {
			display = false;
		}

		/* if display and mouse clicked, add to inventory! */
		if(display && Input.GetMouseButtonDown(0)) {
            Collider item_collider = hit.transform.gameObject.GetComponent<Collider>();
			if(item_renderer) item_renderer.enabled = false;
			if(item_collider) item_collider.enabled = false;
            fieldbook.AddItem(item_data);
		}
	}

	void OnGUI() {
		/* you can only show information about an object if you're in first person mode */
		if(!display || fieldbook.display) return;
		GUI.Label(item_name_rect, item_data.item_name, centered_style);
		GUI.Label(item_description_rect, item_data.description, centered_style);
	}
}
