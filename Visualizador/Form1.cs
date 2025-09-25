using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Visualizador
{
    public partial class Form1 : Form
    {
        // Variables globales
        private string rutaArchivoImportado = string.Empty;
        private string ultimaRutaAbrir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string ultimaRutaGuardar = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string ultimaRutaExportar = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public Form1()
        {            
            InitializeComponent();

            // Configuración inicial del DataGridView
            ConfigurarDataGridView();

            // Conectar eventos extra
            dgvLineas.SelectionChanged += DgvLineas_SelectionChanged;
            txtTraduccion.KeyDown += TxtTraduccion_KeyDown;

            this.KeyPreview = true; // Permite capturar eventos de teclado en el formulario

            this.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            txtOriginal.Multiline = true;
            txtOriginal.ScrollBars = ScrollBars.Vertical;
            txtOriginal.Font = new Font("Segoe UI", 11F);

            txtTraduccion.Multiline = true;
            txtTraduccion.ScrollBars = ScrollBars.Vertical;
            txtTraduccion.Font = new Font("Segoe UI", 11F);
        }

        // =============================
        // CONFIGURACIÓN DE CONTROLES
        // =============================
        private void ConfigurarDataGridView()
        {
            dgvLineas.DefaultCellStyle.Font = new Font("Segoe UI", 11F);
            dgvLineas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            dgvLineas.AllowUserToAddRows = false;
            dgvLineas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLineas.MultiSelect = false;
            dgvLineas.RowHeadersVisible = false;
            dgvLineas.BackgroundColor = Color.White;
            dgvLineas.BorderStyle = BorderStyle.None;
            dgvLineas.AllowUserToResizeRows = false;
            dgvLineas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvLineas.Columns.Clear();
            dgvLineas.Columns.Add("colNumeroLinea", "N° Línea");
            dgvLineas.Columns.Add("colNombre", "Nombre");
            dgvLineas.Columns.Add("colOriginal", "Original");
            dgvLineas.Columns.Add("colTraduccion", "Traducción");

            var colRaw = new DataGridViewTextBoxColumn();
            colRaw.Name = "colRaw";
            colRaw.HeaderText = "RAW";
            colRaw.Visible = false; // columna oculta para almacenar la línea original
            dgvLineas.Columns.Add(colRaw);

            dgvLineas.Dock = DockStyle.Fill; // ocupa todo el espacio disponible

            dgvLineas.Columns["colNumeroLinea"].Width = 60;
            dgvLineas.Columns["colNumeroLinea"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgvLineas.Columns["colNombre"].Width = 200;
            dgvLineas.Columns["colNombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgvLineas.Columns["colOriginal"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvLineas.Columns["colTraduccion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        // =============================
        // IMPORTAR
        // =============================
        private void menuImportar_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos YML (*.yml)|*.yml";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoImportado = ofd.FileName;
                lblNombreArchivo.Text = Path.GetFileName(rutaArchivoImportado);

                CargarArchivo(rutaArchivoImportado);
            }
        }

        private void CargarArchivo(string ruta)
        {
            dgvLineas.Rows.Clear();
            var lineas = File.ReadAllLines(ruta);

            for (int i = 0; i < lineas.Length; i++)
            {
                 string lineaFull = lineas[i]; // mantengo la línea exacta
                string lineaTrim = lineaFull.Trim();

                // Saltar primera línea y comentarios
                if (i == 0 || lineaTrim.StartsWith("#") || string.IsNullOrWhiteSpace(lineaTrim))
                    continue;

                string nombre = "";
                string original = "";
                string traduccion = "";

                try
                {
                    // Buscar el separador de key
                    int idxSeparador = lineaFull.IndexOf(':');
                    if (idxSeparador != -1)
                        nombre = lineaFull.Substring(0, idxSeparador).Trim();

                    // Buscar original → primera comilla después de ':'
                    int idxComilla1 = lineaFull.IndexOf('"', idxSeparador);
                    if (idxComilla1 != -1)
                    {
                        // Si hay traducción, busco la última comilla ANTES del '<'
                        int idxMenor = lineaFull.IndexOf('<', idxComilla1);
                        int idxComilla2;

                        if (idxMenor != -1)
                        {
                            idxComilla2 = lineaFull.LastIndexOf('"', idxMenor);
                        }
                        else
                        {
                            // No hay traducción → busco la última comilla de la línea
                            idxComilla2 = lineaFull.LastIndexOf('"');
                        }

                        if (idxComilla2 > idxComilla1)
                            original = lineaFull.Substring(idxComilla1 + 1, idxComilla2 - idxComilla1 - 1);
                    }

                    // Buscar traducción dentro de < >
                    int idxMenorT = lineaFull.IndexOf('<');
                    int idxMayorT = lineaFull.LastIndexOf('>');
                    if (idxMenorT != -1 && idxMayorT > idxMenorT)
                    {
                        int idxComillaT1 = lineaFull.IndexOf('"', idxMenorT);
                        int idxComillaT2 = lineaFull.LastIndexOf('"', idxMayorT);
                        if (idxComillaT1 != -1 && idxComillaT2 > idxComillaT1)
                            traduccion = lineaFull.Substring(idxComillaT1 + 1, idxComillaT2 - idxComillaT1 - 1);
                    }
                }
                catch
                {
                    // fallback: dejar los campos vacíos si falla
                }

                int rowIndex = dgvLineas.Rows.Add(i + 1, nombre, original, traduccion);
                dgvLineas.Rows[rowIndex].Cells["colRaw"].Value = lineaFull; // guardo la raw para exportar
            }
        }

        // =============================
        // SELECCIONAR FILA
        // =============================
        private void DgvLineas_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvLineas.SelectedRows.Count > 0)
            {
                DataGridViewRow fila = dgvLineas.SelectedRows[0];
                txtOriginal.Text = fila.Cells["colOriginal"].Value?.ToString() ?? "";
                txtTraduccion.Text = fila.Cells["colTraduccion"].Value?.ToString() ?? "";
            }
        }

        // =============================
        // SIGUIENTE
        // =============================
        private void btnSiguiente_Click(object sender, EventArgs e)
        {
            PasarASiguienteFila();
        }

        private void TxtTraduccion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PasarASiguienteFila();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void PasarASiguienteFila()
        {
            if (dgvLineas.CurrentRow != null)
            {
                // Guardar lo escrito en el TextBox
                dgvLineas.CurrentRow.Cells["colTraduccion"].Value = txtTraduccion.Text;

                // Marcar como revisada (naranja)
                dgvLineas.CurrentRow.DefaultCellStyle.BackColor = Color.Orange;

                // Pasar a la siguiente fila
                int filaActual = dgvLineas.CurrentRow.Index;
                if (filaActual < dgvLineas.Rows.Count - 1)
                {
                    dgvLineas.CurrentCell = dgvLineas.Rows[filaActual + 1].Cells[0];
                }
            }
        }

        // =============================
        // CONFIRMAR
        // =============================
        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow fila in dgvLineas.Rows)
            {
                if (fila.DefaultCellStyle.BackColor == Color.Orange)
                {
                    fila.DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }
        }

        // =============================
        // GUARDAR PROGRESO
        // =============================
        private void menuGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoImportado))
            {
                MessageBox.Show("Primero debes importar un archivo.");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Archivo de progreso (*.json)|*.json";
            sfd.InitialDirectory = ultimaRutaGuardar;
            sfd.FileName = Path.GetFileNameWithoutExtension(rutaArchivoImportado) + ".json";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ultimaRutaGuardar = Path.GetDirectoryName(sfd.FileName);

                ProgresoData progreso = new ProgresoData
                {
                    ArchivoOriginal = rutaArchivoImportado,
                    Filas = new List<LineaTraduccion>()
                };

                foreach (DataGridViewRow fila in dgvLineas.Rows)
                {
                    string estado = "Blanco";
                    if (fila.DefaultCellStyle.BackColor == Color.Orange)
                        estado = "Naranja";
                    else if (fila.DefaultCellStyle.BackColor == Color.LightGreen)
                        estado = "Verde";

                    progreso.Filas.Add(new LineaTraduccion
                    {
                        NumeroLinea = Convert.ToInt32(fila.Cells["colNumeroLinea"].Value),
                        Nombre = fila.Cells["colNombre"].Value?.ToString(),
                        Original = fila.Cells["colOriginal"].Value?.ToString(),
                        Traduccion = fila.Cells["colTraduccion"].Value?.ToString(),
                        Estado = estado
                    });
                }

                string json = JsonConvert.SerializeObject(progreso, Formatting.Indented);
                File.WriteAllText(sfd.FileName, json);

                MessageBox.Show("Progreso guardado correctamente.");
            }
        }

        // =============================
        // EXPORTAR
        // =============================
        private void menuExportar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoImportado))
            {
                MessageBox.Show("Primero debes importar un archivo.");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Archivos YML (*.yml)|*.yml";
            sfd.FileName = Path.GetFileName(rutaArchivoImportado);
            sfd.InitialDirectory = ultimaRutaExportar;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string destino = sfd.FileName;
                ultimaRutaExportar = Path.GetDirectoryName(destino);

                var lineasOriginales = File.ReadAllLines(rutaArchivoImportado);
                List<string> nuevasLineas = new List<string>();

                for (int i = 0; i < lineasOriginales.Length; i++)
                {
                    string lineaFull = lineasOriginales[i];

                    // Primera línea, comentarios o vacías → se mantienen igual
                    if (i == 0 || lineaFull.TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(lineaFull))
                    {
                        nuevasLineas.Add(lineaFull);
                        continue;
                    }

                    // Buscar fila correspondiente
                    DataGridViewRow fila = null;
                    foreach (DataGridViewRow f in dgvLineas.Rows)
                    {
                        if (f.Cells["colNumeroLinea"].Value != null &&
                            Convert.ToInt32(f.Cells["colNumeroLinea"].Value) == i + 1)
                        {
                            fila = f;
                            break;
                        }
                    }

                    if (fila == null)
                    {
                        nuevasLineas.Add(lineaFull);
                        continue;
                    }

                    bool confirmada = fila.DefaultCellStyle.BackColor == Color.LightGreen;
                    string traduccion = fila.Cells["colTraduccion"].Value?.ToString() ?? "";
                    string original = fila.Cells["colOriginal"].Value?.ToString() ?? "";
                    string nombre = fila.Cells["colNombre"].Value?.ToString() ?? "";

                    if (confirmada)
                    {
                        string indent = "";
                        int idxFirstChar = lineaFull.IndexOf(lineaFull.TrimStart());
                        if (idxFirstChar >= 0)
                            indent = lineaFull.Substring(0, idxFirstChar);

                        if (!string.IsNullOrEmpty(traduccion))
                        {
                            nuevasLineas.Add($"{indent}{nombre}: \"{traduccion}\"");
                        }
                        else
                        {
                            nuevasLineas.Add($"{indent}{nombre}: \"{original}\"");
                        }
                    }
                    else
                    {
                        nuevasLineas.Add(lineaFull);
                    }
                }

                File.WriteAllLines(destino, nuevasLineas);
                MessageBox.Show("Archivo exportado correctamente.");
            }
        }

        private void menuImportarProgreso_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivo de progreso (*.json)|*.json";
            ofd.InitialDirectory = ultimaRutaAbrir;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoImportado = ofd.FileName;
                ultimaRutaAbrir = Path.GetDirectoryName(rutaArchivoImportado);
                lblNombreArchivo.Text = Path.GetFileName(rutaArchivoImportado);
                CargarArchivo(rutaArchivoImportado);
                string json = File.ReadAllText(ofd.FileName);
                ProgresoData progreso = JsonConvert.DeserializeObject<ProgresoData>(json);

                if (progreso == null)
                {
                    MessageBox.Show("Error al leer el archivo de progreso.");
                    return;
                }

                // Reconstruir archivo original
                rutaArchivoImportado = progreso.ArchivoOriginal;
                lblNombreArchivo.Text = Path.GetFileName(rutaArchivoImportado);

                // Reconstruir filas
                dgvLineas.Rows.Clear();

                foreach (var l in progreso.Filas)
                {
                    int rowIndex = dgvLineas.Rows.Add(
                        l.NumeroLinea, l.Nombre, l.Original, l.Traduccion
                    );

                    if (l.Estado == "Naranja")
                        dgvLineas.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Orange;
                    else if (l.Estado == "Verde")
                        dgvLineas.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }

                MessageBox.Show("Progreso cargado correctamente.");
            
            }
        }

        // Capturar Enter
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PasarASiguienteFila();
                e.Handled = true;   // evita que Windows haga "ding"
                e.SuppressKeyPress = true; // evita que pase el Enter al dgv
            }
        }
    }
}
