using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachOnPrePostRender : MonoBehaviour {
    private int _counter;
    public void OnPreRender() {
        // Do nothing.
        _counter++;
    }

    public void OnPostRender() {
        // Do nothing.
        _counter--;
    }
}
