from login import verify_password
import tkinter as tk
from interface import formBox, button, rightText, dataFilter, canvasMainTable, bttonStatus
# from interface import interface_menu, data_entry, options
from utils import appSize
import os

def app():
    # ---------------------------
    #          App setup
    # ---------------------------

    # Inicialice app
    window = tk.Tk()
    window.withdraw()
    if verify_password(window):
        window.deiconify()
        # Set window app title
        window.title('FP Calzados')
        # Create window-app
        window.geometry(appSize(window))
        # App ico
        # Construimos la ruta absoluta del ícono
        icon_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'img', 'fpimage.ico'))
        print("Icon path:", icon_path)  # Para chequear que esté bien

        window.iconbitmap(icon_path)

        # ------------------
        # Box/Window Setup
        # ------------------

        # Create main frame
        main_frame = formBox(window,
                             relx=0,
                             rely=0,
                             relwidth=0.875,
                             relheight=0.9,
                             bg='blue',
                             highlightbackground="#5D6658",
                             highlightthickness=0.5,
                             anchor='nw')
        # Create filter frame
        filter_frame = formBox(window,
                               relx=0,
                               rely=1.0,
                               relwidth=0.875,
                               relheight=0.1,
                               bg='lightgray',
                               highlightbackground="#5D6658",
                               highlightthickness=0.5,
                               anchor='sw')

        # Create right frame for options
        frame = formBox(window,
                        relx=0.875,
                        rely=0,
                        relwidth=0.125,
                        relheight=1,
                        bg='lightgray',
                        highlightbackground="#5D6658",
                        highlightthickness=0.5)

        # -----------------------------
        #         Button Setup
        # -----------------------------

        # Create a button for Gestion de productos
        button(frame,
               relx=0.5,
               rely=0.2,
               relwidth=1,
               anchor='center',
               text='Gestionar Productos',
               commnand=None,
               relheight=None,
               font='Arial, 10')
        # Create a button for Gestion de ventas
        button(frame,
               relx=0.5,
               rely=0.28,
               relwidth=1,
               anchor='center',
               text='Gestionar Ventas',
               command=None,
               relheight=None,
               font='Arial, 10')
        # Create an exit button
        button(frame,
               relx=0.5,
               rely=0.97,
               relwidth=1,
               anchor='center',
               text='Salir',
               relheight=0.06,
               bg='red',
               fg='black',
               font=('Arial', 16, 'bold italic'),
               bd=5,
               command=lambda: bttonStatus(window,
                                           title='Cerrando programa',
                                           message='¿Estás seguro de que quieres salir?'))

        # -------------------------------
        #       Option Label Setup
        # -------------------------------

        # Create option frame (text and position)
        rightText(frame,
                  relx=0.5,
                  rely=0.1,
                  relwidth=1,
                  relheight=0.1,
                  anchor_geometry="center",
                  text='Menu de opciones',
                  bg='lightgray',
                  fg='black',
                  font=('Arial', 12, 'bold'),
                  anchor='center',
                  wraplength=125,
                  justify='center')

        # Create filter box
        dataFilter(filter_frame)

        # Main box table
        canvasMainTable(main_frame)

        # Close app
        window.protocol("WM_DELETE_WINDOW",
                        lambda: bttonStatus(window,
                                            title='Cerrando programa',
                                            message='¿Estás seguro de que quiere salir?'))

        # Start app
        window.mainloop()
    else:
        window.destroy()


if __name__ == "__main__":
    app()
