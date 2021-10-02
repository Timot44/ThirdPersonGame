using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapons", menuName = "Create Weapons")]
public class WeaponBehavior : ScriptableObject
{
    public string name;
    public int damage;
    public int ammo;
    public float maxFireRate;
    public float currentFireRate;
    public Mesh mesh;
}
