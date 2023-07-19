# Node alpine
FROM node:14-alpine

WORKDIR /usr/app

# On commence par copier le package.json car on ne veut pas faire un npm install pour chaque changement dans l'application
# Car si un changement est détecté au dessus, les autres n'utiliseront pas le cache
COPY package.json .

RUN npm install
# le premier . est le dossier courant (contexte de build: le dossier là où on build l'image)
COPY . .

CMD ["npm", "start"]