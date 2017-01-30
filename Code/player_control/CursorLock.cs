using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Assertions;

public class CursorLock : MonoBehaviour {
    public Texture2D cursor_texture;
	public int cursor_width = 0, cursor_height = 0;

	Texture2D temp_cursor_texture = null;

    Fieldbook fieldbook;
	RigidbodyFirstPersonController controller;

	float forward_speed, backward_speed, strafe_speed, jump_force;
	float sens_x, sens_y;

	void Start() {
        fieldbook = Fieldbook.Instance;
		controller = this.gameObject.GetComponent<RigidbodyFirstPersonController>();

        Assert.IsNotNull(cursor_texture);
        Assert.IsNotNull(controller);
		Assert.IsTrue(cursor_width > 0 && cursor_height > 0);

		forward_speed = controller.movementSettings.ForwardSpeed;
		backward_speed = controller.movementSettings.BackwardSpeed;
		strafe_speed = controller.movementSettings.StrafeSpeed;
		jump_force = controller.movementSettings.JumpForce;

		sens_x = controller.mouseLook.XSensitivity;
		sens_y = controller.mouseLook.YSensitivity;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void OnDestroy() {
		Cursor.lockState = CursorLockMode.None;
		controller = this.gameObject.GetComponent<RigidbodyFirstPersonController>();

		controller.movementSettings.ForwardSpeed = forward_speed;
		controller.movementSettings.BackwardSpeed = backward_speed;
		controller.movementSettings.StrafeSpeed = strafe_speed;
		controller.movementSettings.JumpForce = jump_force;
		controller.mouseLook.XSensitivity = sens_x;
		controller.mouseLook.YSensitivity = sens_y;
	}

	void Update() {
		/* display the cursor if the inventory is enabled */
#if true
		Cursor.visible = fieldbook.display;
#endif
		if(fieldbook.display) {
			Cursor.lockState = CursorLockMode.None;
			controller.movementSettings.ForwardSpeed = 0.0F;
			controller.movementSettings.BackwardSpeed = 0.0F;
			controller.movementSettings.StrafeSpeed = 0.0F;
			controller.movementSettings.JumpForce = 0.0F;
			controller.mouseLook.XSensitivity = 0.0F;
			controller.mouseLook.YSensitivity = 0.0F;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            controller.movementSettings.ForwardSpeed = forward_speed;
			controller.movementSettings.BackwardSpeed = backward_speed;
			controller.movementSettings.StrafeSpeed = strafe_speed;
			controller.movementSettings.JumpForce = jump_force;
			controller.mouseLook.XSensitivity = sens_x;
			controller.mouseLook.YSensitivity = sens_y;
		}
	}

    void OnGUI() {
        if(!fieldbook.display) return;

		GUI.depth = 1000;
        GUI.DrawTexture(
            new Rect(
                Input.mousePosition.x,
                Screen.height - Input.mousePosition.y,
                cursor_width,
				cursor_height
			),
            (temp_cursor_texture == null ? cursor_texture : temp_cursor_texture)
		);
		GUI.depth = 0;
	}

	public void SetCursorTexture(Texture2D new_texture) {
		temp_cursor_texture = new_texture;
    }
}
