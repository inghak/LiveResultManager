using o_bergen.LiveResultManager.Core.Models;
using o_bergen.LiveResultManager.Infrastructure.Destinations;
using o_bergen.LiveResultManager.Infrastructure.Sources;

namespace o_bergen.LiveResultManager.UI;

/// <summary>
/// Form for detecting potential duplicate participants using fuzzy name matching
/// </summary>
public class DuplicatesForm : Form
{
    private readonly AccessDbResultSource _source;
    private readonly SupabaseLookupService? _lookup;
    private DataGridView dgvDuplicates = null!;
    private Button btnFind = null!;
    private Button btnMerge = null!;
    private Button btnClose = null!;
    private Label lblStatus = null!;

    public DuplicatesForm(AccessDbResultSource source, SupabaseLookupService? lookup = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _lookup = lookup;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Duplicate Detection";
        Width = 1200;
        Height = 580;
        MinimumSize = new Size(900, 400);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        lblStatus = new Label
        {
            Location = new Point(12, 14),
            Size = new Size(860, 20),
            Font = new Font("Segoe UI", 9F),
            Text = "Click 'Find Duplicates' to search for potential duplicate participants.",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(lblStatus);

        btnFind = new Button
        {
            Location = new Point(882, 8),
            Size = new Size(100, 28),
            Text = "Find Duplicates",
            UseVisualStyleBackColor = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnFind.Click += async (_, _) => await FindDuplicatesAsync();
        Controls.Add(btnFind);

        btnMerge = new Button
        {
            Location = new Point(988, 8),
            Size = new Size(100, 28),
            Text = "Merge Selected",
            UseVisualStyleBackColor = true,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnMerge.Click += async (_, _) => await MergeSelectedAsync();
        Controls.Add(btnMerge);

        btnClose = new Button
        {
            Location = new Point(1094, 8),
            Size = new Size(84, 28),
            Text = "Close",
            UseVisualStyleBackColor = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        dgvDuplicates = new DataGridView
        {
            Location = new Point(12, 46),
            Size = new Size(1168, 486),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            Font = new Font("Segoe UI", 9F)
        };

        dgvDuplicates.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Merge", HeaderText = "✓", FillWeight = 3, ReadOnly = false });
        dgvDuplicates.Columns.Add(new DataGridViewCheckBoxColumn { Name = "TransferInfo", HeaderText = "Overfør fra ny", FillWeight = 7, ReadOnly = false });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Id", HeaderText = "ID #1", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Konto", HeaderText = "Konto", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Profil", HeaderText = "Profil", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1First", HeaderText = "Fornavn #1", FillWeight = 10, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Last", HeaderText = "Etternavn #1", FillWeight = 12, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Class", HeaderText = "Klasse #1", FillWeight = 6, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1ECard", HeaderText = "Brikke #1", FillWeight = 6, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C1Lop", HeaderText = "Løp #1", FillWeight = 16, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Id", HeaderText = "ID #2", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Konto", HeaderText = "Konto", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Profil", HeaderText = "Profil", FillWeight = 5, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2First", HeaderText = "Fornavn #2", FillWeight = 10, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Last", HeaderText = "Etternavn #2", FillWeight = 12, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Class", HeaderText = "Klasse #2", FillWeight = 6, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2ECard", HeaderText = "Brikke #2", FillWeight = 6, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "C2Lop", HeaderText = "Løp #2", FillWeight = 16, ReadOnly = true });
        dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Likhet", FillWeight = 5, ReadOnly = true });

        dgvDuplicates.CellContentClick += (_, e) =>
        {
            if ((e.ColumnIndex == 0 || e.ColumnIndex == 1) && e.RowIndex >= 0)
            {
                dgvDuplicates.CommitEdit(DataGridViewDataErrorContexts.Commit);
                UpdateMergeButtonState();
            }
        };

        Controls.Add(dgvDuplicates);
    }

    private async Task FindDuplicatesAsync()
    {
        btnFind.Enabled = false;
        lblStatus.Text = "Fetching participants from database...";
        dgvDuplicates.Rows.Clear();

        try
        {
            var participants = await _source.FetchAllParticipantsAsync(CancellationToken.None);
            lblStatus.Text = $"Analysing {participants.Count} participants for duplicates...";

            var pairs = FindDuplicatePairs(participants);

            // Supabase lookup for all unique IDs involved in duplicate pairs
            Dictionary<string, List<string>> competitionsByIds = new();
            HashSet<string> runnerIds = new();
            HashSet<string> profileIds = new();
            if (_lookup != null && _lookup.IsAvailable && pairs.Count > 0)
            {
                var allIds = pairs.SelectMany(p => new[] { p.P1.Id, p.P2.Id }).Distinct().ToList();

                lblStatus.Text = $"Looking up competition entries in Supabase for {pairs.Count} pair(s)...";
                competitionsByIds = await _lookup.FetchCompetitionsByIdsAsync(allIds, CancellationToken.None);

                lblStatus.Text = "Checking runners accounts in Supabase...";
                runnerIds = await _lookup.FetchRunnerIdStatusAsync(allIds, CancellationToken.None);

                lblStatus.Text = "Checking profiles in Supabase...";
                profileIds = await _lookup.FetchProfileRunnerIdsAsync(allIds, CancellationToken.None);
            }

            dgvDuplicates.Rows.Clear();
            foreach (var (p1, p2, score) in pairs)
            {
                var lop1 = competitionsByIds.TryGetValue(p1.Id, out var d1) && d1.Count > 0
                    ? string.Join(", ", d1)
                    : (_lookup?.IsAvailable == true ? "—" : "N/A");

                var lop2 = competitionsByIds.TryGetValue(p2.Id, out var d2) && d2.Count > 0
                    ? string.Join(", ", d2)
                    : (_lookup?.IsAvailable == true ? "—" : "N/A");

                var konto1 = runnerIds.Contains(p1.Id) ? "✓ Konto" : "—";
                var konto2 = runnerIds.Contains(p2.Id) ? "✓ Konto" : "—";
                var profil1 = profileIds.Contains(p1.Id) ? "★ Profil" : "—";
                var profil2 = profileIds.Contains(p2.Id) ? "★ Profil" : "—";

                var rowIdx = dgvDuplicates.Rows.Add(
                    false, false,
                    p1.Id, konto1, profil1, p1.FirstName, p1.LastName, p1.Class, p1.ECard, lop1,
                    p2.Id, konto2, profil2, p2.FirstName, p2.LastName, p2.Class, p2.ECard, lop2,
                    $"{score:P0}");

                var row = dgvDuplicates.Rows[rowIdx];
                var bothHaveAccounts = runnerIds.Contains(p1.Id) && runnerIds.Contains(p2.Id);
                var eitherHasAccount = runnerIds.Contains(p1.Id) || runnerIds.Contains(p2.Id);

                if (bothHaveAccounts)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                else if (eitherHasAccount)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 210);

                if (runnerIds.Contains(p1.Id))
                {
                    row.Cells["C1Konto"].Style.ForeColor = Color.DarkGreen;
                    row.Cells["C1Konto"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                if (runnerIds.Contains(p2.Id))
                {
                    row.Cells["C2Konto"].Style.ForeColor = Color.DarkGreen;
                    row.Cells["C2Konto"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                if (profileIds.Contains(p1.Id))
                {
                    row.Cells["C1Profil"].Style.ForeColor = Color.DarkBlue;
                    row.Cells["C1Profil"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                if (profileIds.Contains(p2.Id))
                {
                    row.Cells["C2Profil"].Style.ForeColor = Color.DarkBlue;
                    row.Cells["C2Profil"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }

            lblStatus.Text = pairs.Count == 0
                ? "Ingen duplikatkandidater funnet."
                : $"Fant {pairs.Count} potensielle duplikatpar.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Feil: {ex.Message}";
            MessageBox.Show(ex.Message, "Feil ved henting av deltakere", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnFind.Enabled = true;
        }
    }

    private void UpdateMergeButtonState()
    {
        var checkedCount = 0;
        foreach (DataGridViewRow row in dgvDuplicates.Rows)
        {
            if (row.Cells["Merge"].Value is true)
                checkedCount++;
        }
        btnMerge.Enabled = checkedCount > 0;
    }

    private async Task MergeSelectedAsync()
    {
        var toMerge = new List<(string KeepId, string RemoveId, bool TransferInfo, bool KeepIdHasAccount, bool RemoveIdHasAccount, string DisplayName)>();

        foreach (DataGridViewRow row in dgvDuplicates.Rows)
        {
            if (row.Cells["Merge"].Value is not true)
                continue;

            var id1 = row.Cells["C1Id"].Value?.ToString() ?? "";
            var id2 = row.Cells["C2Id"].Value?.ToString() ?? "";
            var name1 = $"{row.Cells["C1First"].Value} {row.Cells["C1Last"].Value}";
            var name2 = $"{row.Cells["C2First"].Value} {row.Cells["C2Last"].Value}";
            var konto1 = row.Cells["C1Konto"].Value?.ToString() == "✓ Konto";
            var konto2 = row.Cells["C2Konto"].Value?.ToString() == "✓ Konto";

            if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                continue;

            // Keep the lower numeric ID; fall back to string comparison
            var keepLower = int.TryParse(id1, out var n1) && int.TryParse(id2, out var n2)
                ? n1 <= n2
                : string.Compare(id1, id2, StringComparison.Ordinal) <= 0;

            var keepId = keepLower ? id1 : id2;
            var removeId = keepLower ? id2 : id1;
            var keepName = keepLower ? name1 : name2;
            var removeName = keepLower ? name2 : name1;
            var keepHasAccount = keepLower ? konto1 : konto2;
            var removeHasAccount = keepLower ? konto2 : konto1;
            var transferInfo = row.Cells["TransferInfo"].Value is true;

            var mode = transferInfo ? "(overfør info fra ny)" : "(behold info fra gammel)";
            var accountWarning = (keepHasAccount && removeHasAccount) ? " ⚠ BEGGE HAR KONTO" :
                                 removeHasAccount ? " ℹ konto flyttes fra ny til gammel ID" :
                                 keepHasAccount ? " ℹ konto beholdes på gammel ID" : "";
            toMerge.Add((keepId, removeId, transferInfo, keepHasAccount, removeHasAccount,
                $"Beholder #{keepId} ({keepName}), fjerner #{removeId} ({removeName}) {mode}{accountWarning}"));
        }

        if (toMerge.Count == 0)
            return;

        // Warn specifically about rows where both participants have accounts
        var bothAccountPairs = toMerge.Where(m => m.KeepIdHasAccount && m.RemoveIdHasAccount).ToList();
        if (bothAccountPairs.Count > 0)
        {
            var conflictList = string.Join(Environment.NewLine, bothAccountPairs.Select(m => $"  • {m.DisplayName}"));
            var proceed = MessageBox.Show(
                $"Advarsel: {bothAccountPairs.Count} par har registrerte kontoer på BEGGE ID-er:\n\n{conflictList}\n\nDisse har 18 sesongers historikk knyttet til seg. Kun én konto kan beholdes — den andre vil bli koblet til det beholdte løpernummeret.\n\nVil du fortsette?",
                "⚠ Kontokonflikt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (proceed != DialogResult.Yes)
                return;
        }

        var summary = string.Join(Environment.NewLine, toMerge.Select(m => $"  • {m.DisplayName}"));
        var confirm = MessageBox.Show(
            $"Du er i ferd med å slå sammen {toMerge.Count} par:\n\n{summary}\n\nFortsett?",
            "Bekreft sammenslåing",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        btnMerge.Enabled = false;
        btnFind.Enabled = false;
        int successCount = 0;

        foreach (var (keepId, removeId, transferInfo, keepHasAccount, removeHasAccount, display) in toMerge)
        {
            try
            {
                if (transferInfo)
                {
                    lblStatus.Text = $"Overfører deltakerinfo: #{removeId} → #{keepId}...";

                    if (_lookup is { IsAvailable: true })
                    {
                        // Move removeId's Supabase race entries to keepId (same as standard merge)
                        var updated = await _lookup.ReassignIdAsync(removeId, keepId, CancellationToken.None);
                        if (updated > 0)
                            lblStatus.Text = $"Supabase: oppdaterte {updated} rad(er) for #{removeId} → #{keepId}";

                        if (removeHasAccount)
                        {
                            lblStatus.Text = $"Oppdaterer konto: runner_id #{removeId} → #{keepId}...";
                            await _lookup.UpdateRunnerIdAsync(removeId, keepId, CancellationToken.None);
                        }
                    }

                    // Copy removeId's Name data onto keepId, then merge multi rows + delete removeId
                    await _source.SwapMergeDuplicateAsync(keepId, removeId, CancellationToken.None);
                }
                else
                {
                    lblStatus.Text = $"Slår sammen #{removeId} → #{keepId}...";

                    if (_lookup is { IsAvailable: true })
                    {
                        var updated = await _lookup.ReassignIdAsync(removeId, keepId, CancellationToken.None);
                        if (updated > 0)
                            lblStatus.Text = $"Supabase: oppdaterte {updated} rad(er) for #{removeId} → #{keepId}";

                        // Update runners account if the removed participant had one
                        if (removeHasAccount)
                        {
                            lblStatus.Text = $"Oppdaterer konto: runner_id #{removeId} → #{keepId}...";
                            await _lookup.UpdateRunnerIdAsync(removeId, keepId, CancellationToken.None);
                        }
                    }

                    await _source.MergeDuplicateAsync(keepId, removeId, CancellationToken.None);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Feil ved sammenslåing av #{removeId} → #{keepId}:\n{ex.Message}",
                    "Feil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        lblStatus.Text = $"Sammenslåing ferdig: {successCount} av {toMerge.Count} par slått sammen.";
        btnFind.Enabled = true;

        if (successCount > 0)
        {
            MessageBox.Show(
                $"{successCount} par ble slått sammen.\n\nKjør 'Find Duplicates' på nytt for å se oppdatert liste.",
                "Sammenslåing fullført",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private static List<(ParticipantEntry P1, ParticipantEntry P2, double Score)> FindDuplicatePairs(
        List<ParticipantEntry> participants,
        double threshold = 0.75)
    {
        var pairs = new List<(ParticipantEntry P1, ParticipantEntry P2, double Score)>();

        for (int i = 0; i < participants.Count; i++)
        {
            for (int j = i + 1; j < participants.Count; j++)
            {
                var a = participants[i];
                var b = participants[j];

                // Skip if IDs are identical (same record)
                if (a.Id == b.Id)
                    continue;

                var score = CombinedNameSimilarity(a, b);
                if (score >= threshold)
                    pairs.Add((a, b, score));
            }
        }

        // Sort by score descending
        pairs.Sort((x, y) => y.Score.CompareTo(x.Score));
        return pairs;
    }

    private static double CombinedNameSimilarity(ParticipantEntry a, ParticipantEntry b)
    {
        var firstA = a.FirstName.Trim().ToUpperInvariant();
        var lastA = a.LastName.Trim().ToUpperInvariant();
        var firstB = b.FirstName.Trim().ToUpperInvariant();
        var lastB = b.LastName.Trim().ToUpperInvariant();

        // Normal order
        var normalScore = Similarity(firstA, firstB) * 0.45 + Similarity(lastA, lastB) * 0.55;

        // Swapped order (first/last switched)
        var swappedScore = Similarity(firstA, lastB) * 0.45 + Similarity(lastA, firstB) * 0.55;

        return Math.Max(normalScore, swappedScore);
    }

    /// <summary>
    /// Levenshtein similarity in range [0, 1]
    /// </summary>
    private static double Similarity(string a, string b)
    {
        if (a == b) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;

        int maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)LevenshteinDistance(a, b) / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) dp[i, 0] = i;
        for (int j = 0; j <= n; j++) dp[0, j] = j;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[m, n];
    }
}
