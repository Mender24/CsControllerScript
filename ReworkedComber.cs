using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ReworkedComber : MonoBehaviour
{
    [Header("Combo Settings")]
    public string comboName;              // Last successfully performed combo
    public string needCombo;              // Current required combo
    public List<string> listCombos = new(); // Queue of upcoming combos

    [Header("References")]
    [SerializeField] private LvlSpeedController speedController; // Controls level speed
    [SerializeField] private CameraScript cameraScript;          // Handles camera behavior
    [SerializeField] private TextMeshProUGUI textMeshPro;        // UI element for displaying combos

    // Dictionary: player input -> animation trigger
    private readonly Dictionary<string, string> _comboList = new()
    {
        { "WSWS", "WSWS" }, { "WSWD", "WSWD" }, { "WSWA", "WSWA" }, { "WSAD", "WSAD" },
        { "SSSD", "SSSD" }, { "V-Pose", "V-Pose" }, { "SSSA", "SSSA" }, { "SSS", "SSS" },
        { "SSSS", "SSSS" }, { "SSDW", "SSDW" }, { "SSAW", "SSAW" }, { "SSAD", "SSAD" },
        { "SS", "SS" }, { "S-PoseReverse", "S-PoseReverse" }, { "SSSSS", "SSSSS" }, { "S", "S" },
        { "DWVWD", "DWVWD" }, { "DDD", "DDD" }, { "DD", "DD" }, { "DAD", "DAD" }, { "D", "D" },
        { "AWWA", "AWWA" }, { "ADA", "ADA" }, { "ADAD", "ADAD" }, { "DADA", "DADA" },
        { "AAA", "AAA" }, { "AA", "AA" }, { "A", "A" }, { "DWWD", "DWWD" },
        { "W", "W" }, { "WW", "WW" }, { "WWD", "WWD" }, { "WWA", "WWA" }, { "WWW", "WWW" },
        { "SW", "SW" }, { "DAS", "DAS" }, { "ASDW", "ASDW" }, { "DSAW", "DSAW" }, { "SAD", "SAD" }
    };

    private Animator _animator;
    private string _currentInputBuffer = ""; // Current raw input sequence
    private string _lastValidCombo = "";     // Last valid combo entered
    private string _activePose = "";         // Active animation pose

    private void Start()
    {
        _animator = GetComponentInParent<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"Animator component not found on {gameObject.name}");
        }
    }

    private void Update()
    {
        UnpackCombo();
        HandleInput();
    }
       private void UnpackCombo()
    {
        if (listCombos.Count > 0)
        {
            needCombo = listCombos.First();
        }

        if (string.IsNullOrEmpty(needCombo)) return;

        for (int i = 0; i < needCombo.Length; i++)
        {
            if (_currentInputBuffer.Length > i)
            {
                if (needCombo[i] != _currentInputBuffer[i] || _currentInputBuffer.Length > needCombo.Length)
                {
                    // Wrong input - reset
                    ClearCombo();
                    ClearColors(needCombo.Length);
                    break;
                }
                else
                {
                    Coloring(i);
                }
            }
        }
    }

  
    public void Coloring(int i) // Colors a single character in green when input is correct.
    {
        var charInfo = textMeshPro.textInfo.characterInfo[i];
        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] vertexColors = textMeshPro.textInfo.meshInfo[materialIndex].colors32;
        for (int j = 0; j < 4; j++)
        {
            vertexColors[vertexIndex + j] = Color.green;
        }

        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    public void ClearColors(int length) 
    {
        for (int i = 0; i < length; i++)
        {
            var charInfo = textMeshPro.textInfo.characterInfo[i];
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] vertexColors = textMeshPro.textInfo.meshInfo[materialIndex].colors32; // Resets all characters to white color.
            for (int j = 0; j < 4; j++)
            {
                vertexColors[vertexIndex + j] = Color.white;
            }
        }

        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

   
    public void ShortList() // Removes the first combo from the list.
    {
        if (listCombos.Count > 0)
        {
            listCombos.RemoveAt(0);
        }
        ClearCombo();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            if (other.TryGetComponent(out WallInfo wallScr))
            {
                // Take required combo from the wall
                needCombo = wallScr.wallcombo;
            }
        }
    }

   
    public void InputCombo(string comboInput) // Adds a new combo to the queue.
    {
        listCombos.Add(comboInput);
    }

   
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) AddToBuffer("W");
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) AddToBuffer("A");
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) AddToBuffer("S");
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) AddToBuffer("D");
    }

    private void AddToBuffer(string input)
    {
        _currentInputBuffer += input;
        CheckForValidCombo();
    }

  
    private void CheckForValidCombo()
    {
        if (_comboList.TryGetValue(_currentInputBuffer, out string trigger))
        {
            PlayAnimation(trigger);
            _lastValidCombo = _currentInputBuffer;
            comboName = _lastValidCombo;
        }
        else
        {
            Debug.Log($"Invalid combo: {_currentInputBuffer}");
        }
    }

   
    private void PlayAnimation(string trigger)
    {
        foreach (var combo in _comboList.Values)
        {
            _animator.ResetTrigger(combo);
        }

        _animator.SetTrigger(trigger);
        Debug.Log($"Playing animation: {trigger}");
    }

    
    public void DieAnim()
    {
        ClearCombo();
        _animator.applyRootMotion = true;
        _animator.SetTrigger("Die");
        _animator.SetBool("DieBool", true);
        speedController.moveSpeed = 0;
        cameraScript.isMoving = true;
    }

  
    public void ClearCombo()
    {
        _currentInputBuffer = "";
        _lastValidCombo = "";
        _activePose = "";
    }
}
