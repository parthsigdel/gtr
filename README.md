> ***Gtr's repo is still in its early stages — messy, unorganised, and far from production standard. AI assistance has been kept close to zero so far to maximise learning.***


# 


https://github.com/user-attachments/assets/3c291a2a-8da3-4090-943f-9fc82a9e2efa



## What Gtr is?
A terminal client for GitHub notifications, issues, PRs, and reviews. 

#### Privacy & Trust-First CLI
Your auth token stays local (on your machine). It is never sent to any server — all communication happens directly between your machine and GitHub's API. Nothing is logged, proxied, or stored remotely. Revoke access anytime from your [GitHub settings](https://github.com/settings/applications).

## Current features
-> List notifications, issues, PRs, and reviews

-> View detailed descriptions

-> Open selected notification, issues, pr, or review in your browser. 

-> Close your PR or change it to draft from your terminal. 

-> Mark notification as read (straight from your terminal).

## Installation
### Requirements
Requires - [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later

```
git clone https://github.com/parthsigdel/gtr.git
cd gtr
dotnet run --project src/Gtr 
# or you can use make 
make run 
```


## TODO:
-> Heavy refactor (Crucial)

-> Add tests (Crucial)

