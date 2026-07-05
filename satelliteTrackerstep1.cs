using System;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;

public class Program
{
    static TextBox logBox, detailsBox, searchBox, latBox, lonBox, altBox, azBox, elBox, portBox;
    static ListBox satList;
    static string[] tleLines;
    static Timer timer;
    static SerialPort sp;

    static string selName="", selL1="", selL2="";

    [STAThread]
    public static void Main()
    {
        Form f = new Form();
        f.Text = "Satellite Tracker V5.1 - Internal AZ EL";
        f.Width = 840;
        f.Height = 560;

        Button openTle = new Button(); openTle.Text = "Open TLE"; openTle.Left = 20; openTle.Top = 20; openTle.Width = 90;
        searchBox = new TextBox(); searchBox.Left = 120; searchBox.Top = 22; searchBox.Width = 90; searchBox.Text = "ISS";
        Button searchBtn = new Button(); searchBtn.Text = "Search"; searchBtn.Left = 220; searchBtn.Top = 20; searchBtn.Width = 70;
        Button parseBtn = new Button(); parseBtn.Text = "Select"; parseBtn.Left = 300; parseBtn.Top = 20; parseBtn.Width = 70;
        Button calcBtn = new Button(); calcBtn.Text = "Calc AZ/EL"; calcBtn.Left = 380; calcBtn.Top = 20; calcBtn.Width = 90;

        Label latL = new Label(); latL.Text = "Lat"; latL.Left = 490; latL.Top = 24; latL.Width = 30;
        latBox = new TextBox(); latBox.Left = 520; latBox.Top = 20; latBox.Width = 80; latBox.Text = "36.2877";
        Label lonL = new Label(); lonL.Text = "Lon"; lonL.Left = 610; lonL.Top = 24; lonL.Width = 30;
        lonBox = new TextBox(); lonBox.Left = 640; lonBox.Top = 20; lonBox.Width = 80; lonBox.Text = "59.6127";
        Label altL = new Label(); altL.Text = "Alt"; altL.Left = 730; altL.Top = 24; altL.Width = 30;
        altBox = new TextBox(); altBox.Left = 760; altBox.Top = 20; altBox.Width = 50; altBox.Text = "1000";

        satList = new ListBox(); satList.Left = 20; satList.Top = 60; satList.Width = 220; satList.Height = 340;

        detailsBox = new TextBox();
        detailsBox.Left = 260; detailsBox.Top = 60; detailsBox.Width = 530; detailsBox.Height = 210;
        detailsBox.Multiline = true; detailsBox.ScrollBars = ScrollBars.Vertical;

        Label azL = new Label(); azL.Text = "AZ"; azL.Left = 260; azL.Top = 290;
        azBox = new TextBox(); azBox.Left = 300; azBox.Top = 286; azBox.Width = 90; azBox.Text = "0.0";
        Label elL = new Label(); elL.Text = "EL"; elL.Left = 410; elL.Top = 290;
        elBox = new TextBox(); elBox.Left = 450; elBox.Top = 286; elBox.Width = 90; elBox.Text = "0.0";

        portBox = new TextBox(); portBox.Left = 560; portBox.Top = 286; portBox.Width = 70; portBox.Text = "COM7";
        Button conBtn = new Button(); conBtn.Text = "Connect"; conBtn.Left = 640; conBtn.Top = 284; conBtn.Width = 80;
        Button autoBtn = new Button(); autoBtn.Text = "Auto Track"; autoBtn.Left = 730; autoBtn.Top = 284; autoBtn.Width = 85;

        logBox = new TextBox();
        logBox.Left = 260; logBox.Top = 330; logBox.Width = 530; logBox.Height = 150;
        logBox.Multiline = true; logBox.ScrollBars = ScrollBars.Vertical;

        timer = new Timer(); timer.Interval = 1000;
        timer.Tick += delegate { CalcAzEl(true); };

        openTle.Click += delegate { OpenTle(); };
        searchBtn.Click += delegate { SearchSat(); };
        parseBtn.Click += delegate { SelectSat(); };
        calcBtn.Click += delegate { CalcAzEl(false); };
        conBtn.Click += delegate { ConnectSerial(); };
        autoBtn.Click += delegate { timer.Enabled = !timer.Enabled; Log(timer.Enabled ? "Auto Track ON" : "Auto Track OFF"); };
        satList.SelectedIndexChanged += delegate { SelectSat(); };

        f.Controls.Add(openTle); f.Controls.Add(searchBox); f.Controls.Add(searchBtn); f.Controls.Add(parseBtn); f.Controls.Add(calcBtn);
        f.Controls.Add(latL); f.Controls.Add(latBox); f.Controls.Add(lonL); f.Controls.Add(lonBox); f.Controls.Add(altL); f.Controls.Add(altBox);
        f.Controls.Add(satList); f.Controls.Add(detailsBox);
        f.Controls.Add(azL); f.Controls.Add(azBox); f.Controls.Add(elL); f.Controls.Add(elBox);
        f.Controls.Add(portBox); f.Controls.Add(conBtn); f.Controls.Add(autoBtn);
        f.Controls.Add(logBox);

        Application.Run(f);
    }

    static void OpenTle()
    {
        OpenFileDialog d = new OpenFileDialog();
        d.Filter = "TLE files (*.tle;*.txt)|*.tle;*.txt|All files (*.*)|*.*";
        if (d.ShowDialog() != DialogResult.OK) return;

        tleLines = File.ReadAllLines(d.FileName);
        satList.Items.Clear();

        int count = 0;
        for (int i = 0; i < tleLines.Length - 2; i++)
        {
            string name = tleLines[i].Trim();
            string l1 = tleLines[i + 1];
            string l2 = tleLines[i + 2];

            if (l1.StartsWith("1 ") && l2.StartsWith("2 "))
            {
                satList.Items.Add(name);
                count++;
                i += 2;
            }
        }

        Log("TLE loaded");
        Log("Satellites found: " + count);
    }

    static void SearchSat()
    {
        if (tleLines == null) { Log("Open TLE first"); return; }

        string q = searchBox.Text.ToUpper();
        satList.Items.Clear();
        int count = 0;

        for (int i = 0; i < tleLines.Length - 2; i++)
        {
            string name = tleLines[i].Trim();
            string l1 = tleLines[i + 1];
            string l2 = tleLines[i + 2];

            if (l1.StartsWith("1 ") && l2.StartsWith("2 "))
            {
                if (name.ToUpper().Contains(q))
                {
                    satList.Items.Add(name);
                    count++;
                }
                i += 2;
            }
        }

        Log("Search found: " + count);
        if (count > 0) satList.SelectedIndex = 0;
    }

    static void SelectSat()
    {
        if (!GetSelected(out selName, out selL1, out selL2)) return;

        detailsBox.Text = "Selected: " + selName + "\r\n\r\n" + selL1 + "\r\n" + selL2;
        Log("Selected: " + selName);
    }

    static bool GetSelected(out string name, out string l1, out string l2)
    {
        name = ""; l1 = ""; l2 = "";
        if (tleLines == null || satList.SelectedItem == null) return false;

        string selected = satList.SelectedItem.ToString();

        for (int i = 0; i < tleLines.Length - 2; i++)
        {
            string n = tleLines[i].Trim();
            string a = tleLines[i + 1];
            string b = tleLines[i + 2];

            if (n == selected && a.StartsWith("1 ") && b.StartsWith("2 "))
            {
                name = n; l1 = a; l2 = b;
                return true;
            }
        }
        return false;
    }

    static void CalcAzEl(bool autoSend)
    {
        try
        {
            if (selL1 == "" || selL2 == "") { Log("Select satellite first"); return; }

            double lat = D(latBox.Text) * Math.PI / 180.0;
            double lon = D(lonBox.Text) * Math.PI / 180.0;
            double altKm = D(altBox.Text) / 1000.0;

            double inc = D(selL2.Substring(8, 8)) * Math.PI / 180.0;
            double raan = D(selL2.Substring(17, 8)) * Math.PI / 180.0;
            double ecc = D("0." + selL2.Substring(26, 7).Trim());
            double argp = D(selL2.Substring(34, 8)) * Math.PI / 180.0;
            double m0 = D(selL2.Substring(43, 8)) * Math.PI / 180.0;
            double mm = D(selL2.Substring(52, 11));

            DateTime epoch = EpochToDate(selL1.Substring(18, 14));
            double dt = (DateTime.UtcNow - epoch).TotalSeconds;

            double mu = 398600.4418;
            double n = mm * 2.0 * Math.PI / 86400.0;
            double a = Math.Pow(mu / (n * n), 1.0 / 3.0);

            double M = m0 + n * dt;
            M = NormalizeRad(M);

            double E = M;
            for (int i = 0; i < 10; i++)
                E = M + ecc * Math.Sin(E);

            double xop = a * (Math.Cos(E) - ecc);
            double yop = a * Math.Sqrt(1.0 - ecc * ecc) * Math.Sin(E);

            double cosO = Math.Cos(raan), sinO = Math.Sin(raan);
            double cosi = Math.Cos(inc), sini = Math.Sin(inc);
            double cosw = Math.Cos(argp), sinw = Math.Sin(argp);

            double x = (cosO*cosw - sinO*sinw*cosi)*xop + (-cosO*sinw - sinO*cosw*cosi)*yop;
            double y = (sinO*cosw + cosO*sinw*cosi)*xop + (-sinO*sinw + cosO*cosw*cosi)*yop;
            double z = (sinw*sini)*xop + (cosw*sini)*yop;

            double jd = Julian(DateTime.UtcNow);
            double gmst = GMST(jd);

            double xe = Math.Cos(gmst)*x + Math.Sin(gmst)*y;
            double ye = -Math.Sin(gmst)*x + Math.Cos(gmst)*y;
            double ze = z;

            double re = 6378.137;
            double f = 1.0 / 298.257223563;
            double e2 = f * (2.0 - f);
            double N = re / Math.Sqrt(1.0 - e2 * Math.Sin(lat) * Math.Sin(lat));

            double xo = (N + altKm) * Math.Cos(lat) * Math.Cos(lon);
            double yo = (N + altKm) * Math.Cos(lat) * Math.Sin(lon);
            double zo = (N * (1.0 - e2) + altKm) * Math.Sin(lat);

            double dx = xe - xo;
            double dy = ye - yo;
            double dz = ze - zo;

            double east = -Math.Sin(lon)*dx + Math.Cos(lon)*dy;
            double north = -Math.Sin(lat)*Math.Cos(lon)*dx - Math.Sin(lat)*Math.Sin(lon)*dy + Math.Cos(lat)*dz;
            double up = Math.Cos(lat)*Math.Cos(lon)*dx + Math.Cos(lat)*Math.Sin(lon)*dy + Math.Sin(lat)*dz;

            double range = Math.Sqrt(east*east + north*north + up*up);
            double az = Math.Atan2(east, north) * 180.0 / Math.PI;
            if (az < 0) az += 360.0;
            double el = Math.Asin(up / range) * 180.0 / Math.PI;

            azBox.Text = az.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            elBox.Text = el.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

            detailsBox.Text =
                "Selected: " + selName + "\r\n" +
                "AZ: " + azBox.Text + "\r\n" +
                "EL: " + elBox.Text + "\r\n" +
                "Range km: " + range.ToString("0.0") + "\r\n" +
                "Epoch UTC: " + epoch.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" +
                "Now UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n\r\n" +
                "Note: V5.1 uses simplified internal orbit math.";

            Log("Calc OK AZ=" + azBox.Text + " EL=" + elBox.Text);

            if (autoSend) SendLine("AZ=" + azBox.Text + ",EL=" + elBox.Text);
        }
        catch (Exception ex)
        {
            Log("CALC ERROR: " + ex.Message);
        }
    }

    static DateTime EpochToDate(string s)
    {
        int yy = int.Parse(s.Substring(0, 2));
        double day = D(s.Substring(2));
        int year = yy < 57 ? 2000 + yy : 1900 + yy;
        DateTime d = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return d.AddDays(day - 1.0);
    }

    static double Julian(DateTime dt)
    {
        int Y = dt.Year, M = dt.Month;
        double Dd = dt.Day + dt.Hour/24.0 + dt.Minute/1440.0 + dt.Second/86400.0;
        if (M <= 2) { Y--; M += 12; }
        int A = Y / 100;
        int B = 2 - A + A / 4;
        return Math.Floor(365.25*(Y+4716)) + Math.Floor(30.6001*(M+1)) + Dd + B - 1524.5;
    }

    static double GMST(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double deg = 280.46061837 + 360.98564736629*(jd - 2451545.0) + 0.000387933*T*T - T*T*T/38710000.0;
        deg = deg % 360.0;
        if (deg < 0) deg += 360.0;
        return deg * Math.PI / 180.0;
    }

    static double NormalizeRad(double x)
    {
        x = x % (2.0 * Math.PI);
        if (x < 0) x += 2.0 * Math.PI;
        return x;
    }

    static void ConnectSerial()
    {
        try
        {
            sp = new SerialPort(portBox.Text.Trim(), 115200);
            sp.Open();
            Log("Connected to " + portBox.Text.Trim());
        }
        catch (Exception ex) { Log("SERIAL ERROR: " + ex.Message); }
    }

    static void SendLine(string s)
    {
        if (sp == null || !sp.IsOpen)
        {
            Log("Serial not connected");
            return;
        }
        sp.WriteLine(s);
        Log("Sent: " + s);
    }

    static double D(string s)
    {
        return double.Parse(s.Trim().Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
    }

    static void Log(string s)
    {
        logBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + s + "\r\n");
    }
}
