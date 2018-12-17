# Dev image
FROM node:8.11.3-alpine

WORKDIR /usr/src

COPY package.json package-lock.json ./

RUN npm install

# Set the ENV VAR for PUBLIC_URL before running 'build', which bakes the URL into the output
ARG PUBLIC_URL=http://screenly.staging.clusters.storefront.vpsvc.com/
ENV PUBLIC_URL=$PUBLIC_URL

COPY . .

RUN npm run build

#Production deployment image
FROM node:8.11.3-alpine

ENV PUBLIC_URL=$PUBLIC_URL

RUN npm install -g serve && \
    rm -rf /var/cache/apk/*

WORKDIR /usr/app

COPY --from=0 /usr/src/build .

CMD [ "serve ", "."]
