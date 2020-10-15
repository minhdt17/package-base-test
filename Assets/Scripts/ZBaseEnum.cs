using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZBaseEnum
{
    public enum Status
    {
        installed = 1,
        none = 2,
        updated = 3
    }

    public enum Source
    {
        registry,
        builtin,
        git,
        embedded
    }
}
