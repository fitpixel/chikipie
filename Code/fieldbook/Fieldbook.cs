using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

class Fieldbook : MonoBehaviourSingletonPersistent<Fieldbook> {
	[System.Serializable]
	public class CombinableItem : System.Object {
		public int n_items_needed = 0;
		/* must have the component ItemData and have the tag "item" */
		public GameObject resulting_object;
	}

	[System.Serializable]
	public class CombineSlot : System.Object {
		public int x, y;
		[HideInInspector]
		public bool enabled;
		[HideInInspector]
		public int item; /* < 0 if nothing */
	}

	public CombinableItem[] combinables;
	public CombineSlot[] slots;

	public Texture2D inventory_base_texture;
	public Texture2D inventory_tab_texture;

	public Texture2D inventory_item_outline;
	public Texture2D inventory_item_information_outline;

	public Texture2D inventory_combine_button;
	public Texture2D inventory_combine_button_bad;
	public Texture2D inventory_combine_button_good;
	public Texture2D inventory_combine_slot_enabled;
	public Texture2D inventory_combine_slot_disabled;
	public Texture2D inventory_combined_outline;

	public Texture2D notes_base_texture;
	public Texture2D notes_tab_texture;

	/* how big should both base textures be */
	public int base_width = 0, base_height = 0;

	/* where the boundaries are for the base texture */
	public int inventory_page0_top = 0;
	public int inventory_page0_right = 0;
	public int inventory_page0_bot = 0;
	public int inventory_page0_left = 0;

	/* offsets from the top corner of the base texture */
	public int inventory_tab_x = 0, inventory_tab_y = 0;
	public int notes_tab_x = 0, notes_tab_y = 0;

	/* how big should each items icon be drawn (within the outline texture) */
	public int inventory_item_width = 0, inventory_item_height = 0;
	public int inventory_information_width = 0, inventory_highlight_height = 0;

	/* how much distance should each item outline be from eachother */
	public int inventory_item_inner_pad_x = 0, inventory_item_inner_pad_y = 0;

	/* how much distance should each item outline be from the start / end of the page */
	public int inventory_item_outer_pad_x = 0, inventory_item_outer_pad_y = 0;

	/* how big should each items icon be drawn (within the combine texture) */
	public int inventory_combine_width = 0, inventory_combine_height = 0;
	public int inventory_item_combine_width = 0, inventory_item_combine_height = 0;

	/* where should the combined slot be */
	public int inventory_combined_x = 0, inventory_combined_y = 0;

	/* how big should the resulting item icon be */
	public int inventory_combined_item_width = 0, inventory_combined_item_height = 0;

	/* where should the combine button be */
	public Rect inventory_combine_button_rect;

	[HideInInspector]
	public bool display = false;

	Transform player_transform;

	int selected; /* 0 =  inventory, 1 = notes */
	bool last_mouse_down = false, now_mouse_down = false;

	GUIStyle item_name_style = new GUIStyle();

	Rect base_rect;

	int dragged_item;
	List<ItemData> inventory_items;
	List<ItemData> notes_items;

	CursorLock cursor_lock;

	void Start() {
		/* make this game object persistent across scene changes */
		DontDestroyOnLoad(this.gameObject);

		GameObject[] player_objects = GameObject.FindGameObjectsWithTag("Player");
		Assert.IsTrue(player_objects.Length == 1);
		player_transform = player_objects[0].GetComponent<Transform>();
		Assert.IsNotNull(player_transform);

		/* check if the textures are set in the inspector */
		Assert.IsNotNull(inventory_base_texture);
		Assert.IsNotNull(inventory_tab_texture);
		Assert.IsNotNull(inventory_item_outline);
		Assert.IsNotNull(inventory_item_information_outline);
		Assert.IsNotNull(inventory_combine_button);
		Assert.IsNotNull(inventory_combine_slot_enabled);
		Assert.IsNotNull(inventory_combine_slot_disabled);
		Assert.IsNotNull(inventory_combined_outline);
		Assert.IsNotNull(notes_base_texture);
		Assert.IsNotNull(notes_tab_texture);
		Assert.IsTrue(base_width > 0 && base_height > 0);
		Assert.IsTrue(base_width > 0 && base_height > 0);
		Assert.IsTrue(inventory_item_width > 0 && inventory_item_height > 0);
		Assert.IsTrue(inventory_information_width > 0 && inventory_highlight_height > 0);
		Assert.IsTrue(inventory_item_combine_width > 0 && inventory_item_combine_height > 0);
		Assert.IsTrue(inventory_combine_width > 0 && inventory_combine_height > 0);
		Assert.IsTrue(inventory_combine_button_rect.width > 0 && inventory_combine_button_rect.height > 0);
        Assert.IsTrue(inventory_combined_item_width > 0 && inventory_combined_item_height > 0);

		/* check if each CombinableItem is set up correctly */
		for(int i = 0; i < combinables.Length; ++i) {
			Assert.IsTrue(combinables[i].n_items_needed > 0 && combinables[i].n_items_needed <= slots.Length);
			Assert.IsNotNull(combinables[i].resulting_object);
			Assert.IsTrue(combinables[i].resulting_object.tag == "Item");
			Assert.IsNotNull(combinables[i].resulting_object.GetComponent<ItemData>());
		}

		for(int i = 0; i < slots.Length; ++i) {
			slots[i].enabled = true;
			slots[i].item = -1;
		}

		cursor_lock = GameObject.FindGameObjectWithTag("Player").GetComponent<CursorLock>();
		Assert.IsNotNull(cursor_lock);

		/* calculate the base rectangle for drawing the base texture */
		base_rect = new Rect(
			(Screen.width - base_width) / 2.0F,
			(Screen.height - base_height) / 2.0F,
			base_width,
			base_height
		);

		/* initialize the lists which will store references to the items */
		inventory_items = new List<ItemData>();
		notes_items = new List<ItemData>();

		selected = 0; /* inventory */
		item_name_style.normal.textColor = Color.black;
		dragged_item = -1;
    }

	void Update() {
		if(Input.GetKeyUp(KeyCode.I)) {
			display = !display;
		}
		if(!display) return;

		now_mouse_down = Input.GetMouseButton(0);
	}

	Vector2 GetCursorPosition() {
		return new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
	}

	/* returns -1 if not in combo */
	int WhatComboContainsItem(int item) {
		int j;
		for(j = 0; j < slots.Length; ++j) {
			if(slots[j].item != item) continue;
			break;
		}
		return j < slots.Length ? j : -1;
	}

	void DrawInventory(int relative_x, int relative_y) {
		/* render the base texture */
		GUI.DrawTexture(base_rect, inventory_base_texture, ScaleMode.StretchToFill);

		/* render each item from top to bottom, in a grid */
		int rect_x = relative_x + inventory_page0_left + inventory_item_outer_pad_x;
		int rect_y = relative_y + inventory_page0_top + inventory_item_outer_pad_y;

		int over_inventory_i = -1, over_slot_i = -1;

		/* render each item on grid */
		for(int i = 0; i < inventory_items.Count; ++i) {
			/* draw the outline */
			GUI.DrawTexture(
				new Rect(
					rect_x,
					rect_y,
					inventory_item_outline.width,
					inventory_item_outline.height
				),
				inventory_item_outline,
				ScaleMode.StretchToFill
			);

			if(new Rect(rect_x, rect_y, inventory_item_outline.width, inventory_item_outline.height).
					Contains(GetCursorPosition())) {
				over_inventory_i = i;
			}

			/* do not render dragged items or items within combination slots */
			if(dragged_item != i && WhatComboContainsItem(i) < 0) {
				GUI.DrawTexture(
					new Rect(
						rect_x + (inventory_item_outline.width - inventory_item_width) / 2,
						rect_y + (inventory_item_outline.height - inventory_item_height) / 2,
						inventory_item_width,
						inventory_item_height
					),
					inventory_items[i].icon,
					ScaleMode.StretchToFill
				);
			}

			rect_x += inventory_item_outline.width + inventory_item_inner_pad_x;
			if(rect_x + inventory_item_width + inventory_item_outer_pad_x > relative_x + inventory_page0_right) {
				rect_x = relative_x + inventory_page0_left + inventory_item_outer_pad_x;
				rect_y += inventory_item_outline.height + inventory_item_inner_pad_y;
			}
		}

		/* render combination slots */
		for(int i = 0; i < slots.Length; ++i) {
			Rect slot_rect = new Rect(
				relative_x + slots[i].x,
				relative_y + slots[i].y,
				inventory_combine_width,
				inventory_combine_height
			);

			GUI.DrawTexture(
				slot_rect,
				(slots[i].enabled? inventory_combine_slot_enabled : inventory_combine_slot_disabled),
				ScaleMode.StretchToFill
			);

			/* check if the mouse is over the combo outline */
			if(slot_rect.Contains(GetCursorPosition())) {
				over_slot_i = i;
			}

			if(dragged_item >= 0 && dragged_item == slots[i].item) continue;
			if(slots[i].item < 0) continue;

			GUI.DrawTexture(
				new Rect(
					relative_x + slots[i].x + (inventory_combine_width - inventory_item_combine_width) / 2,
					relative_y + slots[i].y + (inventory_combine_height - inventory_item_combine_height) / 2,
					inventory_item_combine_width,
					inventory_item_combine_height
				),
				inventory_items[slots[i].item].icon,
				ScaleMode.StretchToFill
			);
		}

		/* first time clicked without a dragged item */
		if(dragged_item < 0 && now_mouse_down && !last_mouse_down) {
			int over_item = -1;
			if(over_inventory_i >= 0) {
				/* check if the item is currently in a slot */
				if(WhatComboContainsItem(over_inventory_i) < 0) {
					over_item = over_inventory_i;
                }
			} else if(over_slot_i >= 0) {
				/* check if the item even exists */
				if(slots[over_slot_i].item >= 0) {
					over_item = slots[over_slot_i].item;
                }
			}
			if(over_item >= 0) {
				/* remember what item is selected */
				dragged_item = over_item;
				/* set the cursor to the object's icon */
				cursor_lock.SetCursorTexture(inventory_items[dragged_item].icon);
			}
		}

		/* first time let go with a dragged item */
		if(dragged_item >= 0 && !now_mouse_down && last_mouse_down) {
			/* reset the cursor's icon */
			cursor_lock.SetCursorTexture(null);
			/* if the mouse is outside of the bounds of the inventory, place it in the scene */
			if(!base_rect.Contains(GetCursorPosition())) {
				DropItem(inventory_items[dragged_item]);
			} else {
				/* recover the source slot if it exists */
				int source_slot = WhatComboContainsItem(dragged_item);				
				if(over_slot_i >= 0) { /* destination: slot */
					if(source_slot >= 0) { /* source: slot */
						int source_item = slots[source_slot].item;
						slots[source_slot].item = slots[over_slot_i].item;
						slots[over_slot_i].item = source_item;
					} else { /* source: inventory */
						Debug.Log("placed item '" + inventory_items[dragged_item].item_name + "' from the inventory into a combination slot");
						slots[over_slot_i].item = dragged_item;
					}
				} else if(over_inventory_i >= 0) { /* destination: inventory */
					int dest_slot = WhatComboContainsItem(over_inventory_i);
					if(source_slot >= 0) { /* source: slot */
						Debug.Log("placed item '" + inventory_items[slots[source_slot].item].item_name + "' from a slot into the inventory");
						ItemData source_data = inventory_items[slots[source_slot].item];
						inventory_items[slots[source_slot].item] = inventory_items[over_inventory_i];
						inventory_items[over_inventory_i] = source_data;
						if(dest_slot >= 0 && dest_slot != source_slot) {
							slots[dest_slot].item = slots[source_slot].item;
						}
						slots[source_slot].item = -1;
					} else { /* source: inventory */
						ItemData over_item_data = inventory_items[over_inventory_i];
						inventory_items[over_inventory_i] = inventory_items[dragged_item];
						inventory_items[dragged_item] = over_item_data;
						if(dest_slot >= 0) {
							slots[dest_slot].item = dragged_item;
						}
					}
				}
			}
			dragged_item = -1;
		}

		/* render the combined slot */
		GUI.DrawTexture(
			new Rect(
				relative_x + inventory_combined_x,
				relative_y + inventory_combined_y,
				inventory_combined_outline.width,
				inventory_combined_outline.height
			),
			inventory_combined_outline,
			ScaleMode.StretchToFill
		);

		{
			/* 0 = nothing, 1 = bad, 2 = good */
			int combine_button_state = 0;
			/* check if their ids match */
			bool matching_ids = true;
			int what_id = -1, item_count = 0;
			for(int i = 0; i < slots.Length; ++i) {
				if(slots[i].item < 0) continue;
				++item_count;
                if(what_id < 0) what_id = inventory_items[slots[i].item].id;
				if(inventory_items[slots[i].item].id != what_id) {
					matching_ids = false;
                }
            }
			if(item_count > 0 && (!matching_ids || what_id < 0)) {
				combine_button_state = 1; /* clearly bad! */
			} else if(what_id >= 0 && item_count >= combinables[what_id].n_items_needed) {
				Assert.IsTrue(item_count == combinables[what_id].n_items_needed);
				combine_button_state = 2; /* good! */
			}
			if(combine_button_state == 2) {
				/* display the new item */
				GUI.DrawTexture(
					new Rect(
						relative_x + inventory_combined_x + (inventory_combined_outline.width - inventory_combined_item_width) / 2,
						relative_y + inventory_combined_y + (inventory_combined_outline.height - inventory_combined_item_height) / 2,
						inventory_combined_item_width,
						inventory_combined_item_height
					),
					/* todo(dan): get rid of this GetComponent call, it's slow */
					combinables[what_id].resulting_object.GetComponent<ItemData>().icon,
					ScaleMode.StretchToFill
				);
			}
			/* render the combine button */
			if(GUI.Button(
				new Rect(relative_x + inventory_combine_button_rect.x, relative_y + inventory_combine_button_rect.y, inventory_combine_button_rect.width, inventory_combine_button_rect.height),
				(combine_button_state == 0 ? inventory_combine_button : combine_button_state == 1 ? inventory_combine_button_bad : inventory_combine_button_good),
				GUIStyle.none)) {
				if(combine_button_state == 2) {
					AddItem(combinables[what_id].resulting_object.GetComponent<ItemData>());
					for(int i = 0; i < slots.Length; ++i) {
						if(slots[i].item < 0) continue;
						inventory_items.RemoveRange(slots[i].item, 1);
						slots[i].item = -1;
					}
				}
			}
		}

		/* hovering over an item with mouse up */
		if(!now_mouse_down) {
			bool render = false;
			string item_name = "", item_description = "";
			if(over_inventory_i >= 0) {
				/* check if the item is not being dragged */
				/* check if the item is currently in a slot */
				if(dragged_item != over_inventory_i && WhatComboContainsItem(over_inventory_i) < 0) {
					item_name = inventory_items[over_inventory_i].item_name;
					item_description = inventory_items[over_inventory_i].description;
					render = true;
				}
			} else if(over_slot_i >= 0) {
				/* check if the item even exists */
				/* check if the item is not being dragged */
				if(slots[over_slot_i].item >= 0 && dragged_item != slots[over_slot_i].item) {
					item_name = inventory_items[slots[over_slot_i].item].item_name;
					item_description = inventory_items[slots[over_slot_i].item].description;
					render = true;
				}
			}
			if(render) {
				/* render information */
				GUI.DrawTexture(
					new Rect(
						Input.mousePosition.x,
						Screen.height - Input.mousePosition.y,
						inventory_information_width,
						inventory_highlight_height
					),
					inventory_item_information_outline,
					ScaleMode.StretchToFill
				);
				GUI.Label(
					new Rect(
						10 + Input.mousePosition.x,
						Screen.height - Input.mousePosition.y,
						0,
						0
					),
					item_name,
					item_name_style
				);
				GUI.Label(
					new Rect(
						10 + Input.mousePosition.x,
						Screen.height - Input.mousePosition.y + 20,
						0,
						0
					),
					item_description,
					item_name_style
				);
			}
		}
	}

	void DrawNotes(int relative_x, int relative_y) {
		/* render the base texture */
		GUI.DrawTexture(base_rect, notes_base_texture, ScaleMode.StretchToFill);

		/* render each item */
		for(int i = 0; i < notes_items.Count; ++i) {
			GUI.Label(
				new Rect(
					base_rect.x,
					base_rect.y + i * 20.0F,
					base_rect.width,
					base_rect.height
				),
				notes_items[i].item_name,
				item_name_style
			);
		}
	}

	void OnGUI() {
		if(!display) return;		

		int relative_x = (int)base_rect.x;
		int relative_y = (int)base_rect.y;

		if(selected == 0) { /* inventory */
			DrawInventory(relative_x, relative_y);
        } else if(selected == 1) { /* notes */
			DrawNotes(relative_x, relative_y);
		}

		/* calculate the tab rectangles for drawing the tab textures */
		Rect inventory_tab_rect = new Rect(
			base_rect.x - inventory_tab_texture.width + inventory_tab_x,
			base_rect.y + inventory_tab_y,
			inventory_tab_texture.width,
			inventory_tab_texture.height
		);

		Rect notes_tab_rect = new Rect(
			base_rect.x - notes_tab_texture.width + notes_tab_x,
			base_rect.y + notes_tab_y,
			notes_tab_texture.width,
			notes_tab_texture.height
		);

		/* draw the tabs over the base texture */
		if(GUI.Button(inventory_tab_rect, inventory_tab_texture, GUIStyle.none)) selected = 0;
		else if(GUI.Button(notes_tab_rect, notes_tab_texture, GUIStyle.none)) selected = 1;

		last_mouse_down = now_mouse_down;
	}

	public void AddItem(ItemData a_new_item) {
		Assert.IsNotNull(a_new_item);
		Assert.IsNotNull(a_new_item.icon);
		Assert.IsTrue(a_new_item.id < 0 || (a_new_item.id >= 0 && a_new_item.id < combinables.Length));
		inventory_items.Add(a_new_item);
		Debug.Log("added item '" + a_new_item.item_name + "' to the inventory");
	}

	public void DropItem(ItemData an_existing_item) {
		Assert.IsNotNull(an_existing_item);
		Assert.IsTrue(inventory_items.Contains(an_existing_item));
		Assert.IsTrue(an_existing_item.gameObject.tag == "Item");
		int which_slot = -1;
		for(int i = 0; i < slots.Length; ++i) {
			if(slots[i].item < 0) continue;
			if(inventory_items[slots[i].item] != an_existing_item) continue;
			which_slot = i;
			break;
		}
		Debug.Log("dropped item '" + an_existing_item.item_name + "' from the inventory");
		/* spawn this item in the world */
		/* todo(dan): add physics on items */
		an_existing_item.gameObject.GetComponentInParent<Renderer>().enabled = true;
		an_existing_item.gameObject.GetComponentInParent<Collider>().enabled = true;
		Transform item_transform = an_existing_item.gameObject.GetComponentInParent<Transform>();
		item_transform.position = player_transform.position;
		item_transform.rotation = Quaternion.identity;
		/* remove this item from the inventory */
		inventory_items.Remove(an_existing_item);
		if(which_slot >= 0) {
			slots[which_slot].item = -1;
		}
	}
}