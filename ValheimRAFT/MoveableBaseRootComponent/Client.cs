// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.MoveableBaseRootComponent

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;
using Main = ValheimRAFT.Main;

namespace ValheimRAFT.MoveableBaseRootComponent;

public class Client : MonoBehaviour
{
  private Dictionary RpcHandlers;

  public void Awake()
  {
  }
}