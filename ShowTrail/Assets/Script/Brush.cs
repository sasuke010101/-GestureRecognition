using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Brush : MonoBehaviour {

    public TrailRenderer o;
    public Camera c;
    public GUIText text;

    void Awake() {
        Application.targetFrameRate = 60;
        o.autodestruct = false;
    }

    private List<Vector3> list = new List<Vector3>();
    private bool isStartCounter = false;
    private static Vector3 tempV;
	void Update () {
        text.text = (1f / Time.deltaTime).ToString();

        if (Input.GetMouseButton(0)) {
            Vector3 v = Input.mousePosition;
            v.z = 11;
            tempV = c.ScreenToWorldPoint(v);
            o.transform.position = tempV;
        }
        if (Input.GetMouseButtonDown(0))
        {
            list.Clear();
            StartCoroutine("Counter");
        }
        if (Input.GetMouseButtonUp(0)) {
            StopCoroutine("Counter");
            print(list.Count);
            
        }
	}

    //记录手势
    IEnumerator Counter() {
        while (Operate.mode == RUN_MODE.ACTIVE || Operate.mode == RUN_MODE.LEARNING) {
            list.Add(tempV);
            yield return 0;
        }
    }
}
