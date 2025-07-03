using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PM_Meditation : MonoBehaviour
{
    public float healPercentage;
    //dependencies to the transform/lockout bar
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Meditate()
    {
        StartCoroutine(MeditateCoroutine());
    }

    private IEnumerator MeditateCoroutine()
    {
        //do animation & UI effects
        
        //replenish lockout
        
        yield return null;
    }
}
