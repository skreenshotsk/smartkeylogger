using System;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class ScheduleForm : Form
    {
        public DateTime SelectedDateTime { get; private set; }
        public bool Confirmed { get; private set; }

        private NumericUpDown? numDay;
        private NumericUpDown? numMonth;
        private NumericUpDown? numYear;
        private NumericUpDown? numHour;
        private NumericUpDown? numMinute;
        private Button? btnConfirm;
        private Button? btnCancel;

        public ScheduleForm()
        {
            InitializeComponent();
            Confirmed = false;
        }

        private void InitializeComponent()
        {
            this.Text = "Задать время выполнения";
            this.Size = new System.Drawing.Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = System.Drawing.Color.White;

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Выберите дату и время выполнения:",
                Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            // Дата - День
            Label lblDay = new Label
            {
                Text = "День:",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                Size = new System.Drawing.Size(50, 25),
                Location = new System.Drawing.Point(20, 60)
            };
            this.Controls.Add(lblDay);

            numDay = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 31,
                Value = DateTime.Now.Day,
                Location = new System.Drawing.Point(70, 58),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            this.Controls.Add(numDay);

            // Дата - Месяц
            Label lblMonth = new Label
            {
                Text = "Месяц:",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(140, 60)
            };
            this.Controls.Add(lblMonth);

            numMonth = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 12,
                Value = DateTime.Now.Month,
                Location = new System.Drawing.Point(200, 58),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            this.Controls.Add(numMonth);

            // Дата - Год
            Label lblYear = new Label
            {
                Text = "Год:",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(280, 60)
            };
            this.Controls.Add(lblYear);

            numYear = new NumericUpDown
            {
                Minimum = DateTime.Now.Year,
                Maximum = 2100,
                Value = DateTime.Now.Year,
                Location = new System.Drawing.Point(315, 58),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            this.Controls.Add(numYear);

            // Время - Часы
            Label lblHour = new Label
            {
                Text = "Часы:",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(20, 100)
            };
            this.Controls.Add(lblHour);

            numHour = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 23,
                Value = DateTime.Now.Hour,
                Location = new System.Drawing.Point(70, 98),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            this.Controls.Add(numHour);

            // Время - Минуты
            Label lblMinute = new Label
            {
                Text = "Минуты:",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(130, 100)
            };
            this.Controls.Add(lblMinute);

            numMinute = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Value = DateTime.Now.Minute,
                Location = new System.Drawing.Point(200, 98),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F)
            };
            this.Controls.Add(numMinute);

            // Кнопка Подтвердить
            btnConfirm = new Button
            {
                Text = "Подтвердить",
                Size = new System.Drawing.Size(120, 40),
                Location = new System.Drawing.Point(60, 160),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(33, 150, 243),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            if (btnConfirm != null)
            {
                btnConfirm.Click += BtnConfirm_Click;
                this.Controls.Add(btnConfirm);
            }

            // Кнопка Отмена
            btnCancel = new Button
            {
                Text = "Отмена",
                Size = new System.Drawing.Size(120, 40),
                Location = new System.Drawing.Point(200, 160),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(189, 189, 189),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            try
            {
                if (numDay == null || numMonth == null || numYear == null || numHour == null || numMinute == null)
                    return;
                    
                int day = (int)numDay.Value;
                int month = (int)numMonth.Value;
                int year = (int)numYear.Value;
                int hour = (int)numHour.Value;
                int minute = (int)numMinute.Value;

                SelectedDateTime = new DateTime(year, month, day, hour, minute, 0);
                
                // Проверяем, что дата не в прошлом
                if (SelectedDateTime <= DateTime.Now)
                {
                    MessageBox.Show("Выбранная дата и время должны быть в будущем!", "Ошибка", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Confirmed = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Неверная дата! Проверьте правильность введенных значений.", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

