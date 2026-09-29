using UnityEngine;
using TMPro;
public class BravuraTester : MonoBehaviour
{
    public TMP_Text test;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        test = this.gameObject.GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        //test.text = public TMP_Text counter;
    }
}
