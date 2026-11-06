using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DungeonPauseMenu : MonoBehaviour
{
    public GameObject panel;
    public Behaviour[] playerControls;
    private bool paused;
    private bool[] previousStates;
    private CursorLockMode previousLock;
    private bool previousCursor;

    private void Start()
    {
        Time.timeScale = 1;
        panel.SetActive(false);
        AudioListener.volume = PlayerPrefs.GetFloat("Volume", .8f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (paused) return;
        previousLock = Cursor.lockState;
        previousCursor = Cursor.visible;
        previousStates = new bool[playerControls.Length];
        for (int i = 0; i < playerControls.Length; i++)
        {
            if (playerControls[i] == null) continue;
            previousStates[i] = playerControls[i].enabled;
            playerControls[i].enabled = false;
        }
        paused = true;
        panel.SetActive(true);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Trả lại trạng thái chuột cũ, kể cả khi đang mở Balo.
    public void Resume()
    {
        if (!paused) return;
        for (int i = 0; i < playerControls.Length; i++)
            if (playerControls[i] != null) playerControls[i].enabled = previousStates[i];
        paused = false;
        panel.SetActive(false);
        Time.timeScale = 1;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursor;
    }

    public void MainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy() { Time.timeScale = 1; }
}
