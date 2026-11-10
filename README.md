# Meta XR Unity Project

A collaborative Meta XR project built in Unity. This repository uses Git Large File Storage (LFS) to handle heavy binary assets, 3D models (like `.glb`), textures, and audio.

## ⚠️ Prerequisites

You **must** have Git LFS installed before cloning this repository, or the 3D assets will not download correctly.

1. Download and install [Git LFS](https://git-lfs.github.com/).
2. Open your terminal/PowerShell and initialize it globally:
   ```bash
   git lfs install
## 🚀 Setup & Installation
Clone the repository to your local machine:


git clone [https://github.com/thebuildercore/your-xr-project.git](https://github.com/thebuildercore/your-xr-project.git)
(Git LFS will automatically download the heavy assets in the background).
 
Open Unity Hub, click Add, and select the cloned project folder.

Once opened in Unity, go to Edit > Project Settings > Editor and verify that Asset Serialization Mode is set to Force Text.

## Remote Collaboration Rules
To prevent merge conflicts and broken references, follow these strict guidelines:

Always Pull First: Run git pull before you start working for the day to build on top of the latest changes.

The "One Scene/Prefab" Rule: Unity cannot merge overlapping edits on the exact same scene or prefab. Communicate in chat before modifying a scene to ensure no one else is currently working on it.

Always Commit .meta Files: Unity links everything via .meta files. Always use git add . (or stage all through VS Code) to ensure no .meta files are left behind.

Save Everything Before Committing: Always click File > Save Project in the Unity Editor before checking your Git status so all changes are fully written to your drive.