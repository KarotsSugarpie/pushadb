#!/usr/bin/env python3
"""PushADB Linux preview: Android TV administration through authorized ADB."""
import ipaddress
import os
import pathlib
import queue
import shutil
import subprocess
import threading
import tkinter as tk
from tkinter import ttk, filedialog, messagebox

TOOLS = {
    "IPTV / reboot": [["shell","uptime"],["shell","getprop","sys.boot.reason"],["shell","dumpsys","thermalservice"],["shell","dumpsys","meminfo"],["shell","dumpsys","activity","exit-info"],["logcat","-d","-t","300"]],
    "Network": [["shell","dumpsys","wifi"],["shell","dumpsys","connectivity"]],
    "Screen": [["shell","wm","size"],["shell","wm","density"],["shell","dumpsys","display"]],
    "Audio / codecs": [["shell","dumpsys","audio"],["shell","dumpsys","media.codec"]]
}
TWEAKS = {
    "Sleep 5 minutes": ("system","screen_off_timeout","300000"),
    "Sleep 30 minutes": ("system","screen_off_timeout","1800000"),
    "Animations off": ("global","window_animation_scale","0"),
    "Animations normal": ("global","window_animation_scale","1"),
    "Transitions off": ("global","transition_animation_scale","0"),
    "Transitions normal": ("global","transition_animation_scale","1"),
    "Font normal": ("system","font_scale","1"),
    "Font 130%": ("system","font_scale","1.3")
}
class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("PushADB Linux Preview")
        self.geometry("940x690")
        self.ip = tk.StringVar(value="192.168.254.161")
        self.port = tk.StringVar(value="5555")
        self.apk = tk.StringVar()
        self.tweak = tk.StringVar(value=next(iter(TWEAKS)))
        self.diag = tk.StringVar(value=next(iter(TOOLS)))
        self.q = queue.Queue()
        self.backups = {}
        self.target = None
        self.stop = threading.Event()
        root = ttk.Frame(self, padding=12)
        root.pack(fill="both", expand=True)
        ttk.Label(root,text="PushADB for Linux — Preview",font=("Sans",19,"bold")).pack(anchor="w")
        conn = ttk.LabelFrame(root,text="Connection",padding=8)
        conn.pack(fill="x",pady=5)
        ttk.Entry(conn,textvariable=self.ip,width=20).pack(side="left")
        ttk.Entry(conn,textvariable=self.port,width=7).pack(side="left")
        self.btn(conn,"Connect",self.connect)
        self.btn(conn,"Disconnect",self.disconnect)
        self.btn(conn,"Device info",self.device_info)
        apkbar = ttk.LabelFrame(root,text="APK installation",padding=8)
        apkbar.pack(fill="x",pady=5)
        ttk.Entry(apkbar,textvariable=self.apk,width=54).pack(side="left",fill="x",expand=True)
        self.btn(apkbar,"Browse",lambda: self.apk.set(filedialog.askopenfilename(filetypes=[("Android APK","*.apk")]) or self.apk.get()))
        self.btn(apkbar,"Install / update",self.install)
        tweakbar=ttk.LabelFrame(root,text="TweakUI — reversible",padding=8)
        tweakbar.pack(fill="x",pady=5)
        ttk.Combobox(tweakbar,textvariable=self.tweak,values=list(TWEAKS),state="readonly",width=30).pack(side="left")
        self.btn(tweakbar,"Read",self.read_tweak)
        self.btn(tweakbar,"Apply",self.apply_tweak)
        self.btn(tweakbar,"Undo",self.undo_tweak)
        diagbar=ttk.LabelFrame(root,text="Diagnostics",padding=8)
        diagbar.pack(fill="x",pady=5)
        ttk.Combobox(diagbar,textvariable=self.diag,values=list(TOOLS),state="readonly",width=24).pack(side="left")
        self.btn(diagbar,"Run",self.diagnostics)
        self.btn(diagbar,"Export log",self.export)
        history=ttk.LabelFrame(root,text="ADB log",padding=8)
        history.pack(fill="both",expand=True,pady=5)
        self.output=tk.Text(history,wrap="none",background="#101a28",foreground="#e0e9f4")
        self.output.pack(fill="both",expand=True)
        self.after(100,self.flush)
        self.log("Requires adb (android-tools-adb) and user-authorized device debugging.")
    def btn(self,parent,label,fn):
        ttk.Button(parent,text=label,command=lambda: self.run(fn)).pack(side="left",padx=4)
    def run(self,fn):
        try: fn()
        except Exception as e: messagebox.showerror("PushADB",str(e))
    def log(self,msg): self.q.put(msg)
    def flush(self):
        try:
            while True:
                self.output.insert("end",self.q.get_nowait()+"\n")
                self.output.see("end")
        except queue.Empty: pass
        self.after(100,self.flush)
    def endpoint(self):
        ipaddress.IPv4Address(self.ip.get().strip())
        port=int(self.port.get())
        if not 1<=port<=65535: raise ValueError("Invalid port")
        return f"{self.ip.get().strip()}:{port}"
    def adb(self,*args):
        proc=subprocess.run(["adb",*args],capture_output=True,text=True,timeout=120)
        text=(proc.stdout+"\n"+proc.stderr).strip()
        if proc.returncode: raise RuntimeError(text)
        return text
    def ready(self):
        ep=self.endpoint()
        states=self.adb("devices").splitlines()
        if not any(line.split()[:2]==[ep,"device"] for line in states):
            raise RuntimeError("ADB not authorized. Approve debugging on the TV, then connect again.")
        return ep
    def connect(self):
        ep=self.endpoint()
        def worker():
            try: self.log(self.adb("connect",ep));self.log(self.adb("devices","-l"))
            except Exception as exc: self.log("ERROR: "+str(exc))
        threading.Thread(target=worker,daemon=True).start()
    def disconnect(self):
        ep=self.endpoint()
        self.log(self.adb("disconnect",ep))
    def device_info(self):
        ep=self.ready()
        self.log(self.adb("-s",ep,"shell","getprop","ro.product.model"))
        self.log(self.adb("-s",ep,"shell","getprop","ro.build.version.release"))
    def install(self):
        ep=self.ready(); path=pathlib.Path(self.apk.get())
        if path.suffix.lower()!=".apk" or not path.is_file(): raise ValueError("Select a local APK.")
        if not messagebox.askyesno("Confirm installation",f"Install {path.name} on {ep}? Existing data normally stays intact."): return
        def worker():
            try: self.log("Install: "+self.adb("-s",ep,"install","-r",str(path)))
            except Exception as exc: self.log("ERROR: "+str(exc))
        threading.Thread(target=worker,daemon=True).start()
    def tweak_data(self): return TWEAKS[self.tweak.get()]
    def read_tweak(self):
        ep=self.ready(); ns,key,val=self.tweak_data()
        self.log(f"{ns}/{key}: "+self.adb("-s",ep,"shell","settings","get",ns,key))
    def apply_tweak(self):
        ep=self.ready(); ns,key,val=self.tweak_data()
        before=self.adb("-s",ep,"shell","settings","get",ns,key).strip()
        if not messagebox.askyesno("Confirm tweak",f"{ep}\n{ns}/{key}: {before} -> {val}?"): return
        self.adb("-s",ep,"shell","settings","put",ns,key,val)
        self.backups[(ep,ns,key)]=before
        self.read_tweak()
    def undo_tweak(self):
        ep=self.ready(); ns,key,_=self.tweak_data()
        old=self.backups.get((ep,ns,key))
        if old is None: raise RuntimeError("No previous value saved in this session")
        if not messagebox.askyesno("Restore","Restore previous setting?"): return
        operation=["delete",ns,key] if old=="null" else ["put",ns,key,old]
        self.adb("-s",ep,"shell","settings",*operation)
        del self.backups[(ep,ns,key)]
        self.read_tweak()
    def diagnostics(self):
        ep=self.ready(); chosen=self.diag.get()
        def worker():
            for cmd in TOOLS[chosen]:
                try: self.log("$ "+" ".join(cmd)+"\n"+self.adb("-s",ep,*cmd)[:10000])
                except Exception as exc: self.log("ERROR: "+str(exc))
        threading.Thread(target=worker,daemon=True).start()
    def export(self):
        path=filedialog.asksaveasfilename(defaultextension=".txt",filetypes=[("Text","*.txt")])
        if path: pathlib.Path(path).write_text(self.output.get("1.0","end"),encoding="utf-8")
if __name__ == "__main__":
    if not shutil.which("adb"):
        raise SystemExit("Install ADB first: sudo apt install adb")
    App().mainloop()
