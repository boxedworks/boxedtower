using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{

  public static Vector2 _MouseDownPos, _MouseUpPos, _MouseCurrentPos;

  static bool _Down;
  static int _FirstFingerID;

  public static void HandleInput()
  {
    // Desktop
    if (SystemInfo.deviceType == DeviceType.Desktop)
    {
      // Mouse down
      if (Input.GetMouseButtonDown(0))
      {
        _MouseDownPos = Input.mousePosition;
        Down();
      }
      // Mouse up
      if (Input.GetMouseButtonUp(0))
      {
        _MouseUpPos = Input.mousePosition;
        Up();
      }
      // Mouse move
      if (Input.GetMouseButton(0))
      {
        _MouseCurrentPos = Input.mousePosition;
        Moved();
      }
    }
  }

  static void Down()
  {
    PlayerScript.s_Singleton.MouseDown();
  }

  static void Moved()
  {
    PlayerScript.s_Singleton.MouseMove();
  }

  static void Up()
  {
    PlayerScript.s_Singleton.MouseUp();
  }
}
