namespace MoodyBlues;

public partial class PresetEditorForm : Form
{
    private readonly PlaybackEngine _playbackEngine = new();
    private CancellationTokenSource _playbackCts = new();
    private Preset? _currentPreset;
    private bool _dirty;

    public PresetEditorForm()
    {
        InitializeComponent();
        RefreshPresetList();

        _playbackEngine.OnStarted += () =>
        {
            Invoke(() => { btnPlay.Enabled = false; btnStop.Enabled = true; });
        };
        _playbackEngine.OnFinished += () =>
        {
            Invoke(() => { btnPlay.Enabled = true; btnStop.Enabled = false; });
        };
    }

    // ═══════════════════════════════════════
    //  预设列表（ComboBox 驱动）(⁎⁍̴̛ᴗ⁍̴̛⁎)
    // ═══════════════════════════════════════

    private void RefreshPresetList()
    {
        var selectedId = GetSelectedPresetId();
        cmbPreset.Items.Clear();
        foreach (var p in PresetStore.ListAll())
        {
            cmbPreset.Items.Add(new PresetItem(p.Id, $"{p.Name} ({p.Steps.Count} 步)"));
        }
        // 恢复选择
        for (int i = 0; i < cmbPreset.Items.Count; i++)
        {
            if (cmbPreset.Items[i] is PresetItem item && item.Id == selectedId)
            {
                cmbPreset.SelectedIndex = i;
                return;
            }
        }
    }

    private string? GetSelectedPresetId()
    {
        return cmbPreset.SelectedItem is PresetItem item ? item.Id : null;
    }

    private record PresetItem(string Id, string Display)
    {
        public override string ToString() => Display;
    }

    private void OnPresetSelected(object? sender, EventArgs e)
    {
        var id = GetSelectedPresetId();
        if (id == null) return;

        _currentPreset = PresetStore.Load(id);
        if (_currentPreset == null) return;

        txtPresetName.Text = _currentPreset.Name;
        RefreshGrid();
        _dirty = false;
        SetControlsEnabled(true);
    }

    private void OnNewPreset(object? sender, EventArgs e)
    {
        _currentPreset = new Preset(
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            "新预设",
            DateTime.Now,
            []);
        txtPresetName.Text = _currentPreset.Name;
        dgvSteps.Rows.Clear();
        _dirty = true;
        SetControlsEnabled(true);
        lblStatus.Text = "📝 新建预设 — 点「捕获输入」开始添加步骤";
    }

    private void OnSavePreset(object? sender, EventArgs e)
    {
        if (_currentPreset == null) return;

        var steps = ReadStepsFromGrid();
        _currentPreset = _currentPreset with
        {
            Name = txtPresetName.Text.Trim().Length > 0 ? txtPresetName.Text.Trim() : "未命名",
            Steps = steps
        };
        PresetStore.Save(_currentPreset);
        _dirty = false;
        RefreshPresetList();
        lblStatus.Text = $"✅ 已保存 — {steps.Count} 步";
    }

    private void OnDeletePreset(object? sender, EventArgs e)
    {
        if (_currentPreset == null) return;
        var result = MessageBox.Show(
            $"确定删除预设「{_currentPreset.Name}」吗？",
            "MoodyBlues — 删除预设",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        PresetStore.Delete(_currentPreset.Id);
        _currentPreset = null;
        dgvSteps.Rows.Clear();
        txtPresetName.Text = "";
        RefreshPresetList();
        _dirty = false;
        SetControlsEnabled(false);
        lblStatus.Text = "⏸ 选择一个预设或新建";
    }

    // ═══════════════════════════════════════
    //  步骤编辑
    // ═══════════════════════════════════════

    private void RefreshGrid()
    {
        dgvSteps.Rows.Clear();
        if (_currentPreset == null) return;

        for (int i = 0; i < _currentPreset.Steps.Count; i++)
        {
            var step = _currentPreset.Steps[i];
            var isMouse = step.VkCode is NativeMethods.VK_LBUTTON or NativeMethods.VK_RBUTTON
                or NativeMethods.VK_MBUTTON or NativeMethods.VK_XBUTTON1 or NativeMethods.VK_XBUTTON2;
            var type = isMouse ? "🖱 鼠标" : "⌨ 键盘";
            var coord = isMouse ? $"{step.MouseX},{step.MouseY}" : "-";

            var row = new DataGridViewRow();
            row.CreateCells(dgvSteps, i + 1, step.KeyName, type, step.HoldMs, step.GapMs, coord);
            row.Tag = step;
            dgvSteps.Rows.Add(row);
        }
    }

    private List<PresetStep> ReadStepsFromGrid()
    {
        var steps = new List<PresetStep>();
        foreach (DataGridViewRow row in dgvSteps.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Tag is not PresetStep step) continue;

            int holdMs = step.HoldMs;
            int gapMs = step.GapMs;
            int.TryParse(row.Cells["colHold"].Value?.ToString(), out holdMs);
            int.TryParse(row.Cells["colGap"].Value?.ToString(), out gapMs);
            if (holdMs < 1) holdMs = 1;
            if (gapMs < 0) gapMs = 0;

            steps.Add(step with { HoldMs = holdMs, GapMs = gapMs });
        }
        return steps;
    }

    private void OnCaptureInput(object? sender, EventArgs e)
    {
        using var captureForm = new KeyCaptureForm();
        if (captureForm.ShowDialog(this) != DialogResult.OK) return;

        var step = new PresetStep(
            captureForm.KeyName,
            captureForm.CapturedVk,
            captureForm.CapturedScanCode,
            HoldMs: 100,
            GapMs: 50,
            MouseX: captureForm.CapturedMouseX,
            MouseY: captureForm.CapturedMouseY);

        int insertIndex = dgvSteps.CurrentRow?.Index + 1 ?? dgvSteps.Rows.Count;
        if (insertIndex > dgvSteps.Rows.Count) insertIndex = dgvSteps.Rows.Count;

        var isMouse = captureForm.IsMouse;
        var type = isMouse ? "🖱 鼠标" : "⌨ 键盘";
        var coord = isMouse ? $"{captureForm.CapturedMouseX},{captureForm.CapturedMouseY}" : "-";

        var row = new DataGridViewRow();
        row.CreateCells(dgvSteps, insertIndex + 1, captureForm.KeyName, type, 100, 50, coord);
        row.Tag = step;
        dgvSteps.Rows.Insert(insertIndex, row);

        RenumberRows();
        _dirty = true;
        lblStatus.Text = $"✅ 已捕获：{captureForm.KeyName}{(isMouse ? $" @ ({coord})" : "")}";
    }

    private void OnMoveUp(object? sender, EventArgs e)
    {
        var idx = dgvSteps.CurrentRow?.Index ?? -1;
        if (idx <= 0) return;
        SwapRows(idx, idx - 1);
    }

    private void OnMoveDown(object? sender, EventArgs e)
    {
        var idx = dgvSteps.CurrentRow?.Index ?? -1;
        if (idx < 0 || idx >= dgvSteps.Rows.Count - 1) return;
        SwapRows(idx, idx + 1);
    }

    private void SwapRows(int a, int b)
    {
        var rowA = dgvSteps.Rows[a];
        var rowB = dgvSteps.Rows[b];
        (rowA.Tag, rowB.Tag) = (rowB.Tag, rowA.Tag);
        for (int col = 0; col < dgvSteps.Columns.Count; col++)
            (rowA.Cells[col].Value, rowB.Cells[col].Value) = (rowB.Cells[col].Value, rowA.Cells[col].Value);
        RenumberRows();
        _dirty = true;
    }

    private void OnDeleteStep(object? sender, EventArgs e)
    {
        var idx = dgvSteps.CurrentRow?.Index ?? -1;
        if (idx < 0) return;
        dgvSteps.Rows.RemoveAt(idx);
        RenumberRows();
        _dirty = true;
    }

    private void RenumberRows()
    {
        for (int i = 0; i < dgvSteps.Rows.Count; i++)
            dgvSteps.Rows[i].Cells["colNum"].Value = i + 1;
    }

    private void SetControlsEnabled(bool enabled)
    {
        btnCapture.Enabled = enabled;
        btnMoveUp.Enabled = enabled;
        btnMoveDown.Enabled = enabled;
        btnDeleteStep.Enabled = enabled;
        btnSave.Enabled = enabled;
        btnPlay.Enabled = enabled;
        btnDeletePreset.Enabled = enabled;
    }

    // ═══════════════════════════════════════
    //  播放预设（支持鼠标坐标）(⁎⁍̴̛ᴗ⁍̴̛⁎)
    // ═══════════════════════════════════════

    private async void OnPlayPreset(object? sender, EventArgs e)
    {
        if (_playbackEngine.IsPlaying || _currentPreset == null) return;

        OnSavePreset(sender, e);
        if (_currentPreset == null || _currentPreset.Steps.Count == 0) return;

        var recording = PresetToRecording(_currentPreset);
        _playbackCts = new CancellationTokenSource();
        btnPlay.Enabled = false;
        btnStop.Enabled = true;
        lblStatus.Text = "▶ 播放中...";
        await _playbackEngine.PlayAsync(recording, _playbackCts.Token);
        lblStatus.Text = "⏸ 播放完成";
        btnPlay.Enabled = true;
        btnStop.Enabled = false;
    }

    private void OnStopPlayback(object? sender, EventArgs e)
    {
        _playbackEngine.Stop();
        btnPlay.Enabled = true;
        btnStop.Enabled = false;
        lblStatus.Text = "⏸ 已停止";
    }

    /// <summary>
    /// 将 Preset 转换为 Recording — 鼠标步骤生成 MouseDown/MouseUp + 坐标 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    /// </summary>
    private static Recording PresetToRecording(Preset preset)
    {
        var events = new List<InputEvent>();
        long offset = 0;

        foreach (var step in preset.Steps)
        {
            bool isMouse = step.VkCode is NativeMethods.VK_LBUTTON or NativeMethods.VK_RBUTTON
                or NativeMethods.VK_MBUTTON or NativeMethods.VK_XBUTTON1 or NativeMethods.VK_XBUTTON2;

            if (isMouse)
            {
                // 鼠标：Down + Up 带坐标
                events.Add(new InputEvent(offset, "MouseDown", step.VkCode,
                    step.MouseX, step.MouseY, step.ScanCode));
                events.Add(new InputEvent(offset + step.HoldMs, "MouseUp", step.VkCode,
                    step.MouseX, step.MouseY, step.ScanCode));
            }
            else
            {
                // 键盘：Down + Up
                events.Add(new InputEvent(offset, "KeyDown", step.VkCode,
                    0, 0, step.ScanCode));
                events.Add(new InputEvent(offset + step.HoldMs, "KeyUp", step.VkCode,
                    0, 0, step.ScanCode));
            }

            offset += step.HoldMs + step.GapMs;
        }

        return new Recording(preset.Id, preset.CreatedAt, events, TrackCursor: true);
    }

    private void OnDirtyCheck(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex >= 0)
            _dirty = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_dirty && _currentPreset != null)
        {
            var result = MessageBox.Show(
                "当前预设尚未保存，要保存吗？",
                "MoodyBlues — 预设编辑器",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) OnSavePreset(this, EventArgs.Empty);
            else if (result == DialogResult.Cancel) { e.Cancel = true; return; }
        }
        _playbackEngine.Stop();
        base.OnFormClosing(e);
    }
}
