# Application that show the number of tim that the user visit the website
FROM node:14-alpine

WORKDIR '/app'

COPY package.json .
RUN npm install

COPY . .

CMD ["npm", "start"]