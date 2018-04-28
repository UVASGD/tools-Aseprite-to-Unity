using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class SumGuyTest : MonoBehaviour {

    [HideInInspector]
    public bool isActive;

    [Range(0, 10)]
    public float MoveSpeed = 1;

    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool flipX;
    private bool sitting, guitar;
    private float idleTime, currentMax;
    private const float MAX_IDLETIME = 20f;
    private const float MIN_IDLETIME = 12f;

    private const string Action = "Action";
    private const string Sit = "Sit";
    private const string GetUp = "GetUp";
    private const string Guitar = "Guitar";

    private enum ActionType {
        DEFAULT =0,
        Poke,
        Hit,
        Lenny,
        Spin,
        Laugh,
        Watch
    }

	// Use this for initialization
	void Start () {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () {

        // movement
        bool moving = Mathf.Abs(rb.velocity.x) > .2f;
        anim.SetBool("Moving", moving);
        float speed = Input.GetAxis("Horizontal");
        bool PlayerMoving = Mathf.Abs(speed) > .1;
        if (!sitting && !isActive) {
            if (PlayerMoving) {
                flipX = (speed < 0);
                ResetIdleCounter();
            }
            sr.flipX = flipX;
            if(!guitar)
                rb.velocity = new Vector2(speed* MoveSpeed, rb.velocity.y);
        }
        anim.SetFloat("Speed", PlayerMoving ? Mathf.Abs(speed) : 1);    

        // sitting stuff
        if (Input.GetAxis("Vertical") < 0 && !sitting) {
            ResetIdleCounter();
            Trigger("Sit");
            sitting = true;
        } if(Input.GetAxis("Vertical")>0 && sitting) {
            ResetIdleCounter();
            Trigger("GetUp");
            sitting = false;
        }

        if (Input.GetButtonDown("Interact")) {
            PerformAction(ActionType.DEFAULT);
            guitar = false;
        }

            idleTime -= Time.deltaTime;
        if (idleTime <= 0) {
            anim.SetTrigger("Idle");
            idleTime = currentMax = Random.Range(MIN_IDLETIME, MAX_IDLETIME);
        }

        if (Input.GetButtonDown("Jump")) {
            ResetIdleCounter();
            Trigger(Guitar);
            if (!guitar) guitar = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) PerformAction(ActionType.Poke);
        if (Input.GetKeyDown(KeyCode.Alpha2)) PerformAction(ActionType.Hit);
        if (Input.GetKeyDown(KeyCode.Alpha3)) PerformAction(ActionType.Lenny);
        if (Input.GetKeyDown(KeyCode.Alpha4)) PerformAction(ActionType.Spin);
        if (Input.GetKeyDown(KeyCode.Alpha5)) PerformAction(ActionType.Laugh);
        if (Input.GetKeyDown(KeyCode.Alpha6)) PerformAction(ActionType.Watch);
    }

    private void ResetIdleCounter() {
        idleTime = currentMax;
    }

    private void PerformAction(ActionType action) {
        anim.SetInteger("ActionID", (int)action);
        Trigger(Action);
        isActive = true;
        ResetIdleCounter();
    }

    // triggers one trigger, resets all other triggers
    private void Trigger(string name) {
        if (!name.Equals(Action)) anim.ResetTrigger(Action); else anim.SetTrigger(Action);
        if (!name.Equals(Guitar)) anim.ResetTrigger(Guitar); else anim.SetTrigger(Guitar);
        if (!name.Equals(Sit)) anim.ResetTrigger(Sit); else anim.SetTrigger(Sit);
        if (!name.Equals(GetUp)) anim.ResetTrigger(GetUp); else anim.SetTrigger(GetUp);
    }
}
