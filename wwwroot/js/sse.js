let controller = null;

const statusEl = document.getElementById("status");
const outputEl = document.getElementById("output");
const sendBtn = document.getElementById("sendBtn");
const stopBtn = document.getElementById("stopBtn");
const clearBtn = document.getElementById("clearBtn");

// Campos
const modelEl = document.getElementById("model");
const promptEl = document.getElementById("prompt");
const systemEl = document.getElementById("system");
const suffixEl = document.getElementById("suffix");
const templateEl = document.getElementById("template");
const formatModeEl = document.getElementById("formatMode");
const schemaWrapEl = document.getElementById("schemaWrap");
const formatSchemaEl = document.getElementById("formatSchema");

// Options
const temperatureEl = document.getElementById("temperature");
const numCtxEl = document.getElementById("num_ctx");
const topPEl = document.getElementById("top_p");
const repeatPenaltyEl = document.getElementById("repeat_penalty");

// Mostrar/ocultar área de esquema
formatModeEl.addEventListener("change", () => {
    schemaWrapEl.style.display = (formatModeEl.value === "schema") ? "block" : "none";
});

function setBusy(isBusy) {
    sendBtn.disabled = isBusy;
    stopBtn.disabled = !isBusy;
    statusEl.innerHTML = isBusy
        ? '<span class="spinner"></span>✍️ Escribiendo…'
        : '✅ Listo';
}

function buildBody() {
    const body = {
        model: modelEl.value.trim() || "gpt-oss:20b",
        prompt: promptEl.value.trim(),
        stream: true
    };

    const sys = systemEl.value.trim();
    const sfx = suffixEl.value.trim();
    const tpl = templateEl.value.trim();
    if (sys) body.system = sys;
    if (sfx) body.suffix = sfx;
    if (tpl) body.template = tpl;

    // options (solo incluir si hay valores)
    const options = {};
    if (temperatureEl.value !== "") options.temperature = parseFloat(temperatureEl.value);
    if (numCtxEl.value !== "") options.num_ctx = parseInt(numCtxEl.value, 10);
    if (topPEl.value !== "") options.top_p = parseFloat(topPEl.value);
    if (repeatPenaltyEl.value !== "") options.repeat_penalty = parseFloat(repeatPenaltyEl.value);
    if (Object.keys(options).length > 0) body.options = options;

    // format
    const mode = formatModeEl.value;
    if (mode === "json") {
        body.format = "json";
    } else if (mode === "schema") {
        const raw = formatSchemaEl.value.trim();
        if (raw) {
            try {
                body.format = JSON.parse(raw);
            } catch (e) {
                throw new Error("El esquema JSON en 'Format' no es válido.\n" + e.message);
            }
        }
    }
    return body;
}

async function startStream() {
    outputEl.textContent = "";
    statusEl.textContent = "";
    setBusy(true);

    const body = buildBody();
    if (!body.prompt) {
        setBusy(false);
        statusEl.textContent = "⚠️ El campo 'prompt' es obligatorio.";
        return;
    }

    controller = new AbortController();

    try {
        const response = await fetch("/api/ollama/generate/stream", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
            signal: controller.signal
        });

        if (!response.ok || !response.body) {
            throw new Error("Error HTTP " + response.status);
        }

        // ... dentro de startStream()
        const reader = response.body
            .pipeThrough(new TextDecoderStream())
            .getReader();

        let buffer = "";
        while (true) {
            const { value, done } = await reader.read();
            if (done) break;

            buffer += value;
            const lines = buffer.split("\n");
            buffer = lines.pop() ?? ""; // conservar la última si vino cortada

            for (const line of lines) {
                if (!line.startsWith("data: ")) continue;
                const payload = line.slice(6).trim();

                // Fin del stream
                if (payload === "[DONE]") {
                    setBusy(false);
                    return;
                }

                // Cada línea es un JSON de Ollama
                try {
                    const obj = JSON.parse(payload);

                    // 👇 Mostrar SOLO lo que viene en `response`
                    if (typeof obj.response === "string" && obj.response.length) {
                        outputEl.textContent += obj.response;
                        outputEl.scrollTop = outputEl.scrollHeight;
                    }

                    // (Opcional) Si querés ver el reasoning:
                    // if (obj.thinking) debugPane.textContent += obj.thinking;

                    if (obj.done === true) { // algunos backends envían el objeto final con done=true
                        setBusy(false);
                        return;
                    }
                } catch {
                    // Si no es JSON (o vino una línea de keep-alive), lo ignoramos
                    // console.debug("Linea no JSON:", payload);
                }
            }
        }
    } catch (err) {
        if (err.name === "AbortError") {
            outputEl.textContent += "\n⏹️ Generación detenida por el usuario.\n";
        } else {
            console.error("Stream error:", err);
            statusEl.textContent = "⚠️ Error de conexión.";
        }
    } finally {
        setBusy(false);
    }
}

// Eventos UI
sendBtn.addEventListener("click", startStream);
stopBtn.addEventListener("click", () => { if (controller) controller.abort(); setBusy(false); });
clearBtn.addEventListener("click", () => { outputEl.textContent = ""; statusEl.textContent = "🧹 Pantalla limpia"; });
document.getElementById("prompt").addEventListener("keydown", (e) => {
    if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
        e.preventDefault();
        startStream();
    }
});
