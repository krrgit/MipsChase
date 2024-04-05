using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;
    // [SerializeField] allows variable to be seen in editor but remain private
    [SerializeField] private bool fastRedirect; // FastMove Redirect condition check
    [SerializeField] private float angleDelta; // Delta between m_fAngle & m_fTargetAngle 
    
    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
            
            // Start Dive Animation
            StartCoroutine(IDive());
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void Move()
    {
        // Exit if diving
        if (m_nState == eState.kDiving || m_nState == eState.kRecovering) return;
        
        // Get acceleration direction towards target speed.
        // If faster, slow down. If slower, speed up.
        float dir = Mathf.Sign(m_fTargetSpeed - m_fSpeed); 
        // If redirect, slow down.
        dir = fastRedirect ? -1 : dir;
        
        // Increment m_fSpeed
        m_fSpeed += m_fIncSpeed * dir;
        // Clamp m_fSpeed
        m_fSpeed = Mathf.Clamp(m_fSpeed, 0, m_fMaxSpeed);
        
        // Move
        transform.position += m_fSpeed * -transform.right;
        
        // Set Move State to kMoveFast when at max speed
        if (Mathf.Abs(m_fMaxSpeed - m_fSpeed) <= m_fIncSpeed && m_fSpeed > m_fSlowSpeed)
        {
            m_nState = eState.kMoveFast;
        }
    }

    void Rotate()
    {
        // Exit if diving
        if (m_nState == eState.kDiving || m_nState == eState.kRecovering) return;
        
        // Slow Move: Immediate Turnaround 
        if (m_nState == eState.kMoveSlow)
        {
            m_fAngle = m_fTargetAngle;
        }
        else if (!fastRedirect)
        {
            // Fast Move: Slow Turn
            float dir = Mathf.Sign(m_fTargetAngle - m_fAngle);
            float delta = Mathf.Min(Mathf.Abs(m_fTargetAngle - m_fAngle), m_fFastRotateSpeed);
            m_fAngle += delta * dir;
        }
        else
        {
            // Redirect: keep same face position (m_fAngle = m_fAngle)
        }
        
        // Rotate
        transform.localRotation = Quaternion.Euler(0,0,m_fAngle);
    }

    void FastRedirectCheck()
    {
        // Check if the angle between target and direction is greater than threshold
        angleDelta = Mathf.Abs(m_fAngle - m_fTargetAngle);
        fastRedirect = angleDelta >= m_fFastRotateMax;
        
        // Exit if not in Move Fast
        if (m_nState != eState.kMoveFast) return;
        
        // Redirect
        if (fastRedirect)
        {
            // Update target speed.
            m_fTargetSpeed = m_fSlowSpeed;
            
            // Transition to Move Slow when speed is under slow threshold.
            if (m_fSpeed <= m_fSlowSpeed)
            {
                m_nState = eState.kMoveSlow;
            }
        }
    }

    void Update()
    {
        // Inputs are ideally checked in Update
        // In FixedUpdate inputs can sometimes get "eaten"
        CheckForDive(); 
    }

    void FixedUpdate()
    {
        UpdateDirectionAndSpeed();
        FastRedirectCheck();
        Rotate();
        Move();
        
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }
    
    // Runs separate from Update/FixedUpdate calls.
    IEnumerator IDive()
    {
        // Start Dive 
        m_nState = eState.kDiving;
        float diveTimer = m_fDiveTime;
        
        // Compute speed (in units per second) based on dist/time.
        float speed = m_fDiveDistance / diveTimer; 
        
        // Animate player while timer counts down every frame.
        while (diveTimer > 0)
        {
            // Move player by (speed * Time between frames (~1/60) * facing direction) every frame
            transform.position += speed * Time.deltaTime * -transform.right;
            diveTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        // Wait out recovery time.
        m_nState = eState.kRecovering;
        yield return new WaitForSeconds(m_fDiveRecoveryTime);
        
        // Return to idle.
        m_nState = eState.kMoveSlow;
    }
}
