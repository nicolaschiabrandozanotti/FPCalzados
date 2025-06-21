import tkinter as tk
from tkinter import messagebox
from utils import getSize


# Define all boxes
def formBox(window, **kwargs):
    right_label_keys = ['bg', 'highlightbackground', 'highlightthickness']
    right_geometry_keys = ['relx', 'rely', 'relwidth', 'relheight', 'anchor']
    right_label_values = {}
    right_geometry_values = {}
    for key, value in kwargs.items():
        if key in right_label_keys:
            right_label_values[key] = value
        elif key in right_geometry_keys:
            right_geometry_values[key] = value
    right_frame = tk.Frame(window, **right_label_values)
    right_frame.place(**right_geometry_values)
    return right_frame


# Define all buttons
def button(right_frame, **kwargs):
    button_params = {}
    button_geometry_params = {}
    button_geometry_key = ['relx', 'rely', 'relwidth', 'relheight', 'anchor']
    button_key = ['bg', 'text', 'command', 'font', 'fg', 'bd']
    for key, value in kwargs.items():
        if key in button_geometry_key:
            button_geometry_params[key] = value
        elif key in button_key:
            button_params[key] = value
    btn = tk.Button(right_frame, **button_params)
    btn.place(**button_geometry_params)


# Define all text
def rightText(frame, **kwargs):
    text_params = {}
    geometry_params = {}
    param_keys = ['text', 'bg', 'fg', 'font', 'anchor', 'wraplength', 'justify']
    geometry_keys = ['relx', 'rely', 'relwidth', 'relheight', 'anchor_geometry']
    for key, value in kwargs.items():
        if key in param_keys:
            text_params[key] = value
        elif key in geometry_keys:
            geometry_params['anchor' if key == 'anchor_geometry' else key] = value
    text_label = tk.Label(frame, **text_params)
    text_label.place(**geometry_params)


# Filter placeholder
def filterLabel(filter_entry, placeholder_text):
    def onEntryClick(event):
        if filter_entry.get() == placeholder_text:
            filter_entry.delete(0, 'end')
            filter_entry.config(fg='black')

    def onFocusOut(event):
        if filter_entry.get() == '':
            filter_entry.insert(0, placeholder_text)
            filter_entry.config(fg='grey')

    filter_entry.insert(0, placeholder_text)
    filter_entry.config(fg='grey')
    filter_entry.bind('<FocusIn>', onEntryClick)
    filter_entry.bind('<FocusOut>', onFocusOut)


# Filter area
def dataFilter(frame):
    filter_entry = tk.Entry(frame, font='Arial, 11', bg='lightgray')
    filter_entry.place(relx=1, rely=1, relheight=1, relwidth=1, anchor='se')
    filterLabel(filter_entry, 'Filtrar por...')
    return filter_entry


# Main Table
def canvasMainTable(parent):
    canvas = tk.Canvas(parent, bg='lightblue')
    scroll_y = tk.Scrollbar(parent, orient='vertical', command=canvas.yview, highlightbackground='black',
                            highlightthickness='0.4')
    scroll_x = tk.Scrollbar(parent, orient='horizontal', command=canvas.xview)
    canvas.configure(yscrollcommand=scroll_y.set, xscrollcommand=scroll_x.set)
    canvas.place(relx=0, rely=0, relwidth=1, relheight=1)
    scroll_y.place(relx=1, rely=0, relheight=1, anchor='ne')
    scroll_x.place(relx=0, rely=1, relwidth=0.986, anchor='sw')
    interior = tk.Frame(canvas)
    canvas.create_window((0, 0), window=interior, anchor='nw')
    interior.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))


# Button status
def bttonStatus(window, title, message):
    if messagebox.askyesno(title, message):
        window.destroy()