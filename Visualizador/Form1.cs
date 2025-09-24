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
        public Form1()
        {
            this.KeyPreview = true; // Permite capturar eventos de teclado en el formulario
            InitializeComponent();

            // Configuración inicial del DataGridView
            ConfigurarDataGridView();

            // Conectar eventos extra
            dgvLineas.SelectionChanged += DgvLineas_SelectionChanged;
            txtTraduccion.KeyDown += TxtTraduccion_KeyDown;
        }

        // =============================
        // CONFIGURACIÓN DE CONTROLES
        // =============================
        private void ConfigurarDataGridView()
        {
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
            var parseRegex = new Regex(@"^(\s*)([^:]+)\s*:\s*""([^""]*)""(?:\s*<\s*""([^""]*)""\s*>)?(.*)$");

            for (int i = 0; i < lineas.Length; i++)
            {
                string lineaFull = lineas[i]; // SIN Trim, queremos preservar indentación y formato
                string linea = lineaFull.Trim();

                // Saltar primera línea y comentarios y líneas vacías
                if (i == 0 || linea.StartsWith("#") || string.IsNullOrWhiteSpace(linea))
                    continue;

                string nombre = "";
                string original = "";
                string traduccion = "";

                try
                {
                    var m = parseRegex.Match(lineaFull);
                    if (m.Success)
                    {
                        // m.Groups:
                        // 1 -> leading spaces (si querés usarlos)
                        // 2 -> nombre (key)
                        // 3 -> original (entre comillas)
                        // 4 -> traduccion (opcional)
                        // 5 -> resto de la línea (comentarios, etc)
                        nombre = m.Groups[2].Value.Trim();
                        original = m.Groups[3].Value;
                        traduccion = m.Groups[4].Success ? m.Groups[4].Value : "";
                    }
                    else
                    {
                        // Fallback: intentar extraer nombre con ":" y la primera comilla
                        int idxSeparador = lineaFull.IndexOf(':');
                        if (idxSeparador != -1)
                            nombre = lineaFull.Substring(0, idxSeparador).Trim();

                        int idxComilla1 = lineaFull.IndexOf('"');
                        if (idxComilla1 != -1)
                        {
                            int idxComilla2 = lineaFull.IndexOf('"', idxComilla1 + 1);
                            if (idxComilla2 != -1)
                                original = lineaFull.Substring(idxComilla1 + 1, idxComilla2 - idxComilla1 - 1);
                        }

                        int idxMenor = lineaFull.IndexOf('<');
                        int idxMayor = lineaFull.IndexOf('>');
                        if (idxMenor != -1 && idxMayor != -1)
                        {
                            int idxComillaT1 = lineaFull.IndexOf('"', idxMenor);
                            int idxComillaT2 = lineaFull.LastIndexOf('"', idxMayor);
                            if (idxComillaT1 != -1 && idxComillaT2 != -1 && idxComillaT2 > idxComillaT1)
                                traduccion = lineaFull.Substring(idxComillaT1 + 1, idxComillaT2 - idxComillaT1 - 1);
                        }
                    }
                }
                catch
                {
                    // en caso de error, dejamos campos vacíos pero guardamos raw
                }

                // Agregamos fila: NumeroLinea = i+1 para mantener mapeo con archivo original
                int rowIndex = dgvLineas.Rows.Add(i + 1, nombre, original ?? "", traduccion ?? "");
                // Guardamos la línea raw completa para usarla al exportar y así conservar indentación/trailing comments
                dgvLineas.Rows[rowIndex].Cells["colRaw"].Value = lineaFull;
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
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Archivo de progreso (*.json)|*.json";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
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
            sfd.FileName = Path.GetFileName(rutaArchivoImportado); // mismo nombre que el importado

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string destino = sfd.FileName;
                var lineasOriginales = File.ReadAllLines(rutaArchivoImportado);
                List<string> nuevasLineas = new List<string>();

                // Regex para reemplazar la parte de "original" y el <"..."> opcional,
                // manteniendo la indentación y el resto de la línea (comentarios).
                var replaceRegex = new Regex(@"^(\s*[^:]+:\s*)""[^""]*""(?:\s*<\s*""[^""]*""\s*>)?(.*)$");

                for (int i = 0; i < lineasOriginales.Length; i++)
                {
                    string lineaFull = lineasOriginales[i];

                    // Primera línea, comentarios o vacías → se mantienen igual
                    if (i == 0 || lineaFull.TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(lineaFull))
                    {
                        nuevasLineas.Add(lineaFull);
                        continue;
                    }

                    // Buscar fila correspondiente (por número de línea)
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
                        // No está en la grilla → dejamos la línea original íntegra
                        nuevasLineas.Add(lineaFull);
                        continue;
                    }

                    bool confirmada = fila.DefaultCellStyle.BackColor == Color.LightGreen;
                    string traduccion = fila.Cells["colTraduccion"].Value?.ToString() ?? "";
                    string original = fila.Cells["colOriginal"].Value?.ToString() ?? "";

                    if (confirmada)
                    {
                        if (!string.IsNullOrEmpty(traduccion))
                        {
                            // Reemplazamos el texto entre comillas y borramos la sección <"...">, preservando indentación y resto
                            var m = replaceRegex.Match(lineaFull);
                            if (m.Success)
                            {
                                string left = m.Groups[1].Value; // incluye indentación y "key: "
                                string tail = m.Groups[2].Value; // resto (comentarios, espacios)
                                string nuevaLinea = $"{left}\"{traduccion}\"{tail}";
                                nuevasLineas.Add(nuevaLinea);
                            }
                            else
                            {
                                // Fallback: si no hace match por algún motivo, construimos simple manteniendo key básico
                                string nombre = fila.Cells["colNombre"].Value?.ToString() ?? "";
                                string indent = "";
                                // intentar sacar indentación desde la línea original
                                int idxFirstChar = lineasOriginales[i].IndexOf(lineasOriginales[i].TrimStart());
                                if (idxFirstChar >= 0)
                                    indent = lineasOriginales[i].Substring(0, idxFirstChar);
                                nuevasLineas.Add($"{indent}{nombre}: \"{traduccion}\"");
                            }
                        }
                        else
                        {
                            // Confirmada pero traducción vacía → eliminar la parte <"..."> dejando el original intacto
                            string sinAngle = Regex.Replace(lineaFull, @"\s*<\s*""[^""]*""\s*>", "");
                            nuevasLineas.Add(sinAngle);
                        }
                    }
                    else
                    {
                        // No confirmada → dejar tal cual
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

            if (ofd.ShowDialog() == DialogResult.OK)
            {
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
