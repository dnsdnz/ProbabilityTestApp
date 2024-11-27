using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources", menuName = "Input Validator", order = 100)]
public class InputValidator : TMP_InputValidator
{
    public override char Validate(ref string text, ref int pos, char ch)
    {
        if (char.IsDigit(ch) || ch == '-' || ch == '.' || ch == ',' || ch == '/')
        {
            return ch; 
        }

        return '\0'; 
    }
}