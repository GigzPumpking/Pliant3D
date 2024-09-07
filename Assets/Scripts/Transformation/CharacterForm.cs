using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CharacterForm : ScriptableObject
{
    public Form[] form;

    public int formCount => form.Length;

    public Form GetForm(int index) => form[index];
}
