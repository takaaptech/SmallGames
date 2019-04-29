using UnityEngine;


[System.Serializable]
public class InputManager : BaseManager<InputManager> {
    [HideInInspector] public float horizontal; //Float that stores horizontal input
    [HideInInspector] public float vertical; //Float that stores horizontal input
    [HideInInspector] public bool fireHeld; //Bool that stores jump pressed
    [HideInInspector] public bool firePressed; //Bool that stores jump held

    bool readyToClear; //Bool used to keep input in sync

    void ProcessInputs(){
        //Accumulate horizontal axis input
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        //Accumulate button inputs
        firePressed = Input.GetButtonDown("Jump");
        fireHeld = Input.GetButton("Jump");
    }


    public override void DoUpdate(float deltaTime){
        ClearInput();

        if (Main.IsGameOver())
            return;

        ProcessInputs();

        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vertical = Mathf.Clamp(vertical, -1f, 1f);
    }

    //public override void DoFixedUpdate(){
    //    readyToClear = true;
    //}

    void ClearInput(){
        if (!readyToClear)
            return;
        //Reset all inputs
        horizontal = 0;
        vertical = 0;
        firePressed = false;
        fireHeld = false;

        readyToClear = false;
    }
}