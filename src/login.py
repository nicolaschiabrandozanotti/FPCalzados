import tkinter as tk
from tkinter import messagebox
import bcrypt
import os
from PIL import Image, ImageTk


class LoginWindow(tk.Toplevel):
    def __init__(self, master=None):
        super().__init__(master)
        self.title("Iniciar sesión")
        self.geometry("325x100")
        self.resizable(False, False)
        # Load and resize images using Pillow
        o_eye_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'img', 'eye_open.png'))
        h_eye_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'img', 'eye_closed.png'))

        # Open with Pillow and resize (Ex: 20x20 px)
        open_img_pil = Image.open(o_eye_path).resize((20, 20), Image.Resampling.LANCZOS)
        closed_img_pil = Image.open(h_eye_path).resize((20, 20), Image.Resampling.LANCZOS)

        # Convert image for compatibility with tkinter
        self.eye_open_img = load_icon(o_eye_path)
        self.eye_closed_img = load_icon(h_eye_path)

        # Entry password
        self.password_entry = tk.Entry(self, show='*', width=25)
        self.password_entry.pack(side='left', padx=(20, 0), pady=20)
        self.password_entry.focus_set()
        self.password_entry.bind('<Return>', self.check_password_event)
        # Button toggle password visibility
        self.show_password = False
        self.toggle_btn = tk.Button(self, image=self.eye_closed_img, command=self.toggle_password)
        self.toggle_btn.pack(side='left', padx=10)

        # Button submit
        self.submit_btn = tk.Button(self, text="Iniciar Sesión", command=self.check_password)
        self.submit_btn.pack(side='left')

        self.attempts = 0
        self.max_attempts = 3

        self.protocol("WM_DELETE_WINDOW", self.on_close)

        self.center_window()

    def check_password_event(self, event):
        self.check_password()

    # Button hide and show passw
    def toggle_password(self):
        if self.show_password:
            self.password_entry.config(show='*')
            self.toggle_btn.config(image=self.eye_closed_img)
            self.show_password = False
        else:
            self.password_entry.config(show='')
            self.toggle_btn.config(image=self.eye_open_img)
            self.show_password = True

    # Box placing
    def center_window(self):
        self.update_idletasks()
        width = self.winfo_width()
        height = self.winfo_height()
        screen_width = self.winfo_screenwidth()
        screen_height = self.winfo_screenheight()
        x = (screen_width // 2) - (width // 2)
        y = (screen_height // 2) - (height // 2)
        self.geometry(f'{width}x{height}+{x}+{y}')

    # Validate password
    def check_password(self):
        # Read stored hash
        try:
            with open(AUTH_PATH, 'rb') as f:
                stored_hash = f.read().strip()
        except FileNotFoundError:
            messagebox.showerror("Error", "El archivo de la contraseña no se pudo encontrar.")
            self.destroy()
            return

        entered_password = self.password_entry.get()

        if bcrypt.checkpw(entered_password.encode('utf-8'), stored_hash):
            self.destroy()  # Close login window, password correct
        else:
            self.attempts += 1
            remaining = self.max_attempts - self.attempts
            self.password_entry.config(state='disabled')
            if remaining > 0:
                messagebox.showwarning("Contraseña Incorrecta",
                                       f"Contraseña errónea. {remaining} intentos restantes.")
            else:
                messagebox.showerror("Limite de intentos fallidos",
                                     "Demasiados intentos fallidos. Cerrando la aplicación.")
                self.master.destroy()  # Close main app window
                self.destroy()
            self.password_entry.config(state='normal')
            self.password_entry.delete(0, tk.END)
            self.password_entry.focus_set()

    def on_close(self):
        # Handle closing login window (exit app)
        self.master.destroy()
        self.destroy()


def verify_password(master):
    login = LoginWindow(master)
    master.wait_window(login)  # Wait until login window is closed
    return True


# Get passw path
AUTH_PATH = os.path.join(os.path.dirname(__file__), '..', 'secrets', 'auth.cfg')


def load_icon(path, size=(20, 20)):
    img = Image.open(path).convert("RGBA").resize(size, Image.Resampling.LANCZOS)

    # White background
    background = Image.new("RGBA", img.size, (255, 255, 255, 255))
    background.paste(img, mask=img.split()[3])  # Paste using alpha channel as mask

    # Convert to RGB
    img_no_alpha = background.convert("RGB")

    return ImageTk.PhotoImage(img_no_alpha)
