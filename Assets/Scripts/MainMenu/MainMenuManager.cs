using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // 如果你用的是默认传输层，若是WebSocket则引用对应的

public class MainMenuManager : MonoBehaviour
{
    public TMPro.TMP_InputField ipInput;
    public TMPro.TMP_Dropdown playerCountDropdown;

    public void OnJoinGameClicked()
    {
        // 存入静态变量，方便下个场景调用
        StaticGameSettings.TargetServerIP = ipInput.text;
        StaticGameSettings.DesiredPlayerCount = playerCountDropdown.value + 1; 

        // 跳转场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}