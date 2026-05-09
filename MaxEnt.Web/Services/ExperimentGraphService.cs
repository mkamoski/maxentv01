using System.Globalization;
using System.Text;

namespace MaxEnt.Web.Services;

/// <summary>
/// Generates paper-style SVG graphs matching the figures in
/// "Provably Efficient Maximum Entropy Exploration" (arXiv:1812.02690).
/// Figure 2a-c: entropy of the policy over epochs (MaxEnt vs random baseline).
/// Figure 2d-f: log-probability of 2-D state-space occupancy heatmap.
/// </summary>
public static class ExperimentGraphService
{
    // ── canvas ────────────────────────────────────────────────────────
    private const int W = 600, H = 420;
    // PadL: room for y-tick labels; PadT: title + sub-header + legend row; PadB: x-tick labels + axis label
    private const int PadL = 70, PadR = 20, PadT = 80, PadB = 70;
    private const int PlotW = W - PadL - PadR;
    private const int PlotH = H - PadT - PadB;

    private static string Fmt(double v, string fmt = "G4") => v.ToString(fmt, CultureInfo.InvariantCulture);
    private static string N(int v) => v.ToString(CultureInfo.InvariantCulture);

    // ── public API ────────────────────────────────────────────────────

    /// <summary>
    /// Policy Entropy over Epochs — Figure 2a/2b/2c of the paper (primary graph type).
    /// Blue = MaxEnt Agent, Orange = Random Policy.
    /// X = Number of Epochs (0, 5, 10, 15 …).  Y = Policy Entropy H(π).
    /// </summary>
    public static string PolicyEntropyOverEpochs(
        string source,
        IReadOnlyList<double> maxEntEntropies,
        IReadOnlyList<double> randomEntropies)
    {
        var sb = new StringBuilder();
        OpenSvg(sb);

        AppendTitle(sb, source + " \u2014 Policy Entropy over Epochs");
        AppendSubHeader(sb,
            "X: number of epochs (count) \u00b7 Y: policy entropy H(\u03c0) \u00b7 " +
            "blue = MaxEnt Agent, orange = Random Policy");
        AppendLineLegend(sb,
            new[] { "MaxEnt Agent", "Random Policy" },
            new[] { "#2171b5", "#f16913" });   // medium blue / medium orange

        int epochCount = Math.Max(maxEntEntropies.Count, randomEntropies.Count);

        double rawMaxY = 0.01;
        foreach (var v in maxEntEntropies) if (v > rawMaxY) rawMaxY = v;
        foreach (var v in randomEntropies) if (v > rawMaxY) rawMaxY = v;
        double maxY = NiceMax(rawMaxY, 5);

        // x step: prefer 5 if there are at most 50 epochs, else pick a nice step
        double xStep = epochCount <= 50 ? 5 : NiceStep(epochCount, 8);
        double yStep = NiceStep(maxY, 5);

        AppendPlotBackground(sb);
        AppendXTicksEpochs(sb, epochCount, xStep);
        AppendYTicksEntropy(sb, maxY, yStep);

        AppendLine(sb, randomEntropies, epochCount, maxY, "#f16913");
        AppendLine(sb, maxEntEntropies, epochCount, maxY, "#2171b5");

        CloseSvg(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Entropy over epochs — Figure 2a/2b/2c style.
    /// X = episode number (unitless count).  Y = policy entropy H(π) in nats.
    /// </summary>
    public static string EntropyOverEpochs(
        string source,
        IReadOnlyList<double> maxEntEntropies,
        IReadOnlyList<double> randomEntropies)
    {
        var sb = new StringBuilder();
        OpenSvg(sb);

        // ── header ────────────────────────────────────────────────────
        AppendTitle(sb, source + " \u2014 Entropy of Policy over Epochs");
        AppendSubHeader(sb, "X: episode number (count) \u00b7 Y: policy entropy H(\u03c0) in nats (higher = more exploration)");
        AppendLineLegend(sb,
            new[] { "MaxEnt agent (blue)", "Random baseline (orange)" },
            new[] { "#1f77b4", "#ff7f0e" });

        // ── scale ─────────────────────────────────────────────────────
        int episodeCount = Math.Max(maxEntEntropies.Count, randomEntropies.Count);

        double rawMaxY = 0.01;
        foreach (var v in maxEntEntropies) if (v > rawMaxY) rawMaxY = v;
        foreach (var v in randomEntropies) if (v > rawMaxY) rawMaxY = v;
        double maxY = NiceMax(rawMaxY, 5);   // rounded ceiling with 5% headroom built in

        double xStep = NiceStep(episodeCount, 6);
        double yStep = NiceStep(maxY, 5);

        // ── plot area ─────────────────────────────────────────────────
        AppendPlotBackground(sb);
        AppendXTicks(sb, 0, episodeCount, xStep, "episodes");
        AppendYTicks(sb, 0, maxY, yStep, "nats");

        // ── data ──────────────────────────────────────────────────────
        AppendLine(sb, randomEntropies, episodeCount, maxY, "#ff7f0e");
        AppendLine(sb, maxEntEntropies, episodeCount, maxY, "#1f77b4");

        CloseSvg(sb);
        return sb.ToString();
    }

    /// <summary>
    /// 2-D state occupancy heatmap — Figure 2d/2e/2f style.
    /// xMin/xMax and yMin/yMax are the real-world physical ranges so tick labels show actual values.
    /// </summary>
    public static string StateOccupancyHeatmap(
        string source,
        Dictionary<(int x, int y), int> visits,
        string xLabel, double xMin, double xMax,
        string yLabel, double yMin, double yMax,
        int bins = 20)
    {
        var sb = new StringBuilder();
        OpenSvg(sb);

        // ── header ────────────────────────────────────────────────────
        AppendTitle(sb, source + " \u2014 Log-Probability of State Occupancy");
        AppendSubHeader(sb, "X: " + xLabel + " \u00b7 Y: " + yLabel + " \u00b7 colour: log P(s) (yellow = most visited)");
        AppendHeatmapHeaderLegend(sb);

        // ── cells ─────────────────────────────────────────────────────
        double cellW = (double)PlotW / bins;
        double cellH = (double)PlotH / bins;
        int maxCount = 1;
        foreach (var v in visits.Values) if (v > maxCount) maxCount = v;

        for (int bx = 0; bx < bins; bx++)
        {
            for (int by = 0; by < bins; by++)
            {
                visits.TryGetValue((bx, by), out int count);
                double logP = count > 0 ? Math.Log(count + 1.0) / Math.Log(maxCount + 1.0) : 0;
                double cx = PadL + bx * cellW;
                double cy = PadT + (bins - 1 - by) * cellH;
                sb.Append("<rect x='").Append(Fmt(cx, "F1"))
                  .Append("' y='").Append(Fmt(cy, "F1"))
                  .Append("' width='").Append(Fmt(cellW, "F1"))
                  .Append("' height='").Append(Fmt(cellH, "F1"))
                  .Append("' fill='").Append(HeatColor(logP)).Append("'/>\n");
            }
        }

        // ── axes with real-world tick values ─────────────────────────
        AppendPlotBorder(sb);
        double xStep = NiceStep(xMax - xMin, 5);
        double yStep = NiceStep(yMax - yMin, 5);
        AppendXTicksRange(sb, xMin, xMax, xStep, xLabel);
        AppendYTicksRange(sb, yMin, yMax, yStep, yLabel);

        CloseSvg(sb);
        return sb.ToString();
    }

    // ── SVG primitives ────────────────────────────────────────────────

    private static void OpenSvg(StringBuilder sb) =>
        sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='")
          .Append(N(W)).Append("' height='").Append(N(H))
          .Append("' style='background:#fff;font-family:sans-serif;font-size:11px;'>\n");

    private static void CloseSvg(StringBuilder sb) => sb.Append("</svg>");

    private static void AppendTitle(StringBuilder sb, string title) =>
        sb.Append("<text x='").Append(N(W / 2))
          .Append("' y='18' text-anchor='middle' font-size='13' font-weight='bold' fill='#222'>")
          .Append(Esc(title)).Append("</text>\n");

    private static void AppendSubHeader(StringBuilder sb, string text) =>
        sb.Append("<text x='").Append(N(W / 2))
          .Append("' y='34' text-anchor='middle' font-size='10' fill='#666' font-style='italic'>")
          .Append(Esc(text)).Append("</text>\n");

    // Horizontal swatch legend, centred at y=52
    private static void AppendLineLegend(StringBuilder sb, string[] labels, string[] colours)
    {
        const int itemW = 160;
        int totalW = labels.Length * itemW;
        int startX = (W - totalW) / 2;
        const int ly = 52;
        for (int i = 0; i < labels.Length; i++)
        {
            int lx = startX + i * itemW;
            sb.Append("<line x1='").Append(N(lx)).Append("' y1='").Append(N(ly))
              .Append("' x2='").Append(N(lx + 22)).Append("' y2='").Append(N(ly))
              .Append("' stroke='").Append(colours[i]).Append("' stroke-width='2.5'/>\n");
            sb.Append("<text x='").Append(N(lx + 27)).Append("' y='").Append(N(ly + 4))
              .Append("' font-size='11' fill='#333'>").Append(Esc(labels[i])).Append("</text>\n");
        }
    }

    // Horizontal colour-ramp legend for heatmap, centred at y=52
    private static void AppendHeatmapHeaderLegend(StringBuilder sb)
    {
        const int rampW = 160, rampH = 10;
        int rx = (W - rampW) / 2;
        const int ry = 46;
        sb.Append("<defs><linearGradient id='hramp' x1='0' y1='0' x2='1' y2='0'>")
          .Append("<stop offset='0%' stop-color='#000080'/>")
          .Append("<stop offset='33%' stop-color='#0080ff'/>")
          .Append("<stop offset='66%' stop-color='#80ff80'/>")
          .Append("<stop offset='100%' stop-color='#ffff00'/>")
          .Append("</linearGradient></defs>\n");
        sb.Append("<text x='").Append(N(rx - 4)).Append("' y='").Append(N(ry + 9))
          .Append("' font-size='10' fill='#555' text-anchor='end'>low</text>\n");
        sb.Append("<rect x='").Append(N(rx)).Append("' y='").Append(N(ry))
          .Append("' width='").Append(N(rampW)).Append("' height='").Append(N(rampH))
          .Append("' fill='url(#hramp)' stroke='#aaa' stroke-width='0.5'/>\n");
        sb.Append("<text x='").Append(N(rx + rampW + 4)).Append("' y='").Append(N(ry + 9))
          .Append("' font-size='10' fill='#555'>high log P(s)</text>\n");
    }

    private static void AppendPlotBackground(StringBuilder sb) =>
        sb.Append("<rect x='").Append(N(PadL)).Append("' y='").Append(N(PadT))
          .Append("' width='").Append(N(PlotW)).Append("' height='").Append(N(PlotH))
          .Append("' fill='#f8f8f8' stroke='#ccc'/>\n");

    private static void AppendPlotBorder(StringBuilder sb) =>
        sb.Append("<rect x='").Append(N(PadL)).Append("' y='").Append(N(PadT))
          .Append("' width='").Append(N(PlotW)).Append("' height='").Append(N(PlotH))
          .Append("' fill='none' stroke='#888' stroke-width='1'/>\n");

    // X-axis ticks + labels for line chart (0 … maxVal)
    private static void AppendXTicks(StringBuilder sb, double minVal, double maxVal, double step, string uom)
    {
        // axis label (with UOM) below the plot
        sb.Append("<text x='").Append(N(PadL + PlotW / 2)).Append("' y='").Append(N(H - 6))
          .Append("' text-anchor='middle' font-size='12' fill='#444'>Episode (").Append(Esc(uom)).Append(")</text>\n");

        double v = 0;
        while (v <= maxVal + step * 0.01)
        {
            double px = PadL + (v / maxVal) * PlotW;
            // grid line
            sb.Append("<line x1='").Append(Fmt(px, "F1")).Append("' y1='").Append(N(PadT))
              .Append("' x2='").Append(Fmt(px, "F1")).Append("' y2='").Append(N(PadT + PlotH))
              .Append("' stroke='#ddd'/>\n");
            // tick mark
            sb.Append("<line x1='").Append(Fmt(px, "F1")).Append("' y1='").Append(N(PadT + PlotH))
              .Append("' x2='").Append(Fmt(px, "F1")).Append("' y2='").Append(N(PadT + PlotH + 4))
              .Append("' stroke='#888'/>\n");
            // tick label
            sb.Append("<text x='").Append(Fmt(px, "F1")).Append("' y='").Append(N(PadT + PlotH + 16))
              .Append("' text-anchor='middle' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    // Y-axis ticks + labels for line chart (0 … maxVal)
    private static void AppendYTicks(StringBuilder sb, double minVal, double maxVal, double step, string uom)
    {
        // axis label rotated
        sb.Append("<text transform='rotate(-90)' x='").Append(N(-(PadT + PlotH / 2)))
          .Append("' y='16' text-anchor='middle' font-size='12' fill='#444'>H(\u03c0) (").Append(Esc(uom)).Append(")</text>\n");

        double v = 0;
        while (v <= maxVal + step * 0.01)
        {
            double py = PadT + PlotH - (v / maxVal) * PlotH;
            sb.Append("<line x1='").Append(N(PadL)).Append("' y1='").Append(Fmt(py, "F1"))
              .Append("' x2='").Append(N(PadL + PlotW)).Append("' y2='").Append(Fmt(py, "F1"))
              .Append("' stroke='#ddd'/>\n");
            sb.Append("<line x1='").Append(N(PadL - 4)).Append("' y1='").Append(Fmt(py, "F1"))
              .Append("' x2='").Append(N(PadL)).Append("' y2='").Append(Fmt(py, "F1"))
              .Append("' stroke='#888'/>\n");
            sb.Append("<text x='").Append(N(PadL - 7)).Append("' y='").Append(Fmt(py + 4, "F1"))
              .Append("' text-anchor='end' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    // X-axis ticks for heatmap — maps bin 0…bins to minVal…maxVal
    private static void AppendXTicksRange(StringBuilder sb, double minVal, double maxVal, double step, string uom)
    {
        sb.Append("<text x='").Append(N(PadL + PlotW / 2)).Append("' y='").Append(N(H - 6))
          .Append("' text-anchor='middle' font-size='12' fill='#444'>").Append(Esc(uom)).Append("</text>\n");

        double range = maxVal - minVal;
        double v = CeilToStep(minVal, step);
        while (v <= maxVal + step * 0.01)
        {
            double px = PadL + ((v - minVal) / range) * PlotW;
            sb.Append("<line x1='").Append(Fmt(px, "F1")).Append("' y1='").Append(N(PadT + PlotH))
              .Append("' x2='").Append(Fmt(px, "F1")).Append("' y2='").Append(N(PadT + PlotH + 4))
              .Append("' stroke='#888'/>\n");
            sb.Append("<text x='").Append(Fmt(px, "F1")).Append("' y='").Append(N(PadT + PlotH + 16))
              .Append("' text-anchor='middle' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    // Y-axis ticks for heatmap
    private static void AppendYTicksRange(StringBuilder sb, double minVal, double maxVal, double step, string uom)
    {
        sb.Append("<text transform='rotate(-90)' x='").Append(N(-(PadT + PlotH / 2)))
          .Append("' y='16' text-anchor='middle' font-size='12' fill='#444'>").Append(Esc(uom)).Append("</text>\n");

        double range = maxVal - minVal;
        double v = CeilToStep(minVal, step);
        while (v <= maxVal + step * 0.01)
        {
            double py = PadT + PlotH - ((v - minVal) / range) * PlotH;
            sb.Append("<line x1='").Append(N(PadL - 4)).Append("' y1='").Append(Fmt(py, "F1"))
              .Append("' x2='").Append(N(PadL)).Append("' y2='").Append(Fmt(py, "F1"))
              .Append("' stroke='#888'/>\n");
            sb.Append("<text x='").Append(N(PadL - 7)).Append("' y='").Append(Fmt(py + 4, "F1"))
              .Append("' text-anchor='end' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    // X ticks for PolicyEntropyOverEpochs — label is "Number of Epochs"
    private static void AppendXTicksEpochs(StringBuilder sb, double maxVal, double step)
    {
        sb.Append("<text x='").Append(N(PadL + PlotW / 2)).Append("' y='").Append(N(H - 6))
          .Append("' text-anchor='middle' font-size='12' fill='#444'>Number of Epochs</text>\n");

        double v = 0;
        while (v <= maxVal + step * 0.01)
        {
            double px = PadL + (v / maxVal) * PlotW;
            sb.Append("<line x1='").Append(Fmt(px, "F1")).Append("' y1='").Append(N(PadT))
              .Append("' x2='").Append(Fmt(px, "F1")).Append("' y2='").Append(N(PadT + PlotH))
              .Append("' stroke='#ddd'/>\n");
            sb.Append("<line x1='").Append(Fmt(px, "F1")).Append("' y1='").Append(N(PadT + PlotH))
              .Append("' x2='").Append(Fmt(px, "F1")).Append("' y2='").Append(N(PadT + PlotH + 4))
              .Append("' stroke='#888'/>\n");
            sb.Append("<text x='").Append(Fmt(px, "F1")).Append("' y='").Append(N(PadT + PlotH + 16))
              .Append("' text-anchor='middle' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    // Y ticks for PolicyEntropyOverEpochs — label is "Policy Entropy"
    private static void AppendYTicksEntropy(StringBuilder sb, double maxVal, double step)
    {
        sb.Append("<text transform='rotate(-90)' x='").Append(N(-(PadT + PlotH / 2)))
          .Append("' y='16' text-anchor='middle' font-size='12' fill='#444'>Policy Entropy H(\u03c0)</text>\n");

        double v = 0;
        while (v <= maxVal + step * 0.01)
        {
            double py = PadT + PlotH - (v / maxVal) * PlotH;
            sb.Append("<line x1='").Append(N(PadL)).Append("' y1='").Append(Fmt(py, "F1"))
              .Append("' x2='").Append(N(PadL + PlotW)).Append("' y2='").Append(Fmt(py, "F1"))
              .Append("' stroke='#ddd'/>\n");
            sb.Append("<line x1='").Append(N(PadL - 4)).Append("' y1='").Append(Fmt(py, "F1"))
              .Append("' x2='").Append(N(PadL)).Append("' y2='").Append(Fmt(py, "F1"))
              .Append("' stroke='#888'/>\n");
            sb.Append("<text x='").Append(N(PadL - 7)).Append("' y='").Append(Fmt(py + 4, "F1"))
              .Append("' text-anchor='end' font-size='10' fill='#555'>").Append(FormatTickVal(v)).Append("</text>\n");
            v += step;
        }
    }

    private static void AppendLine(StringBuilder sb, IReadOnlyList<double> values,
                                   double maxX, double maxY, string colour)
    {
        if (values.Count == 0) return;
        var pts = new StringBuilder();
        for (int i = 0; i < values.Count; i++)
        {
            double px = PadL + ((double)i / Math.Max(values.Count - 1, 1)) * PlotW;
            double py = PadT + PlotH - (Math.Min(values[i], maxY) / maxY) * PlotH;
            pts.Append(Fmt(px, "F1")).Append(',').Append(Fmt(py, "F1")).Append(' ');
        }
        sb.Append("<polyline points='").Append(pts)
          .Append("' fill='none' stroke='").Append(colour)
          .Append("' stroke-width='2'/>\n");
    }

    // ── scaling helpers ───────────────────────────────────────────────

    /// <summary>Round up to a "nice" ceiling that is at least 5% above rawMax.</summary>
    private static double NiceMax(double rawMax, int targetTicks)
    {
        double padded = rawMax * 1.08;  // 8% headroom so the top line is never clipped
        double step = NiceStep(padded, targetTicks);
        return Math.Ceiling(padded / step) * step;
    }

    /// <summary>Choose a human-friendly step size so there are roughly targetTicks intervals.</summary>
    private static double NiceStep(double range, int targetTicks)
    {
        if (range <= 0) return 1;
        double raw = range / targetTicks;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double nice = norm switch
        {
            <= 1.0 => 1.0,
            <= 2.0 => 2.0,
            <= 5.0 => 5.0,
            _      => 10.0
        };
        return nice * mag;
    }

    private static double CeilToStep(double v, double step) =>
        Math.Ceiling(v / step) * step;

    private static string FormatTickVal(double v)
    {
        // Show integer if it is whole, otherwise up to 3 sig figs
        if (v == Math.Floor(v)) return ((long)v).ToString(CultureInfo.InvariantCulture);
        return v.ToString("G3", CultureInfo.InvariantCulture);
    }

    private static string HeatColor(double t)
    {
        t = Math.Clamp(t, 0, 1);
        int r, g, b;
        if (t < 0.33)
        {
            double s = t / 0.33;
            r = 0; g = (int)(s * 128); b = (int)(128 + s * 127);
        }
        else if (t < 0.66)
        {
            double s = (t - 0.33) / 0.33;
            r = (int)(s * 128); g = (int)(128 + s * 127); b = (int)(255 - s * 255);
        }
        else
        {
            double s = (t - 0.66) / 0.34;
            r = (int)(128 + s * 127); g = 255; b = 0;
        }
        return "rgb(" + r.ToString(CultureInfo.InvariantCulture)
             + "," + g.ToString(CultureInfo.InvariantCulture)
             + "," + b.ToString(CultureInfo.InvariantCulture) + ")";
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;");
}
