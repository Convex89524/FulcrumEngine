// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 (GPLv3 only).
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Veldrid;

namespace Fulcrum.Engine.Render;

public class MultiChannelTexturedRenderable : GeometryRenderable
{
    private readonly int _channelCount;
    private readonly TextureView[] _channelViews;
    private ResourceLayout _channelLayout;
    private ResourceSet _channelSet;

    /// <param name="channelCount">通道数（建议 2~4）。</param>
    public MultiChannelTexturedRenderable(
        string name,
        int channelCount,
        VertexLayoutDescription vertexLayout,
        GraphicsPipelineDescription pipelineDescription)
        : base(name, vertexLayout, pipelineDescription)
    {
        if (channelCount <= 0) throw new ArgumentOutOfRangeException(nameof(channelCount));
        _channelCount = channelCount;
        _channelViews = new TextureView[_channelCount];
    }

    /// <summary>设置指定通道的纹理视图（0-based）。</summary>
    public void SetChannelView(int index, TextureView view)
    {
        if ((uint)index >= (uint)_channelCount) throw new ArgumentOutOfRangeException(nameof(index));
        _channelViews[index] = view;
        if (_graphicsDevice != null && _channelLayout != null)
        {
            _channelSet?.Dispose();
            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
        }
    }

    /// <summary>批量设置通道纹理。</summary>
    public void SetChannelViews(params TextureView[] views)
    {
        if (views == null || views.Length != _channelCount)
            throw new ArgumentException($"需要提供 {_channelCount} 个通道纹理。");
        for (int i = 0; i < _channelCount; i++) _channelViews[i] = views[i];
        if (_graphicsDevice != null && _channelLayout != null)
        {
            _channelSet?.Dispose();
            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
        }
    }

    protected override void CreatePipeline(ResourceFactory factory)
    {
        var elements = new List<ResourceLayoutElementDescription>(_channelCount * 2);
        for (int i = 0; i < _channelCount; i++)
        {
            elements.Add(new ResourceLayoutElementDescription($"Channel{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment));
            elements.Add(new ResourceLayoutElementDescription($"Channel{i}Sampler", ResourceKind.Sampler, ShaderStages.Fragment));
        }
        _channelLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(elements.ToArray()));

        var pipeDesc = PipelineDescription;
        var layouts = pipeDesc.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
        layouts.Add(_channelLayout);
        pipeDesc.ResourceLayouts = layouts.ToArray();
        PipelineDescription = pipeDesc;

        base.CreatePipeline(factory);

        _channelSet?.Dispose();
        _channelSet = factory.CreateResourceSet(new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
    }

    private BindableResource[] BuildResourceSetParams()
    {
        if (_graphicsDevice == null) return Array.Empty<BindableResource>();
        var list = new List<BindableResource>(_channelCount * 2);
        for (int i = 0; i < _channelCount; i++)
        {
            if (_channelViews[i] == null)
                throw new InvalidOperationException($"Channel {i} 纹理尚未设置。");
            list.Add(_channelViews[i]);
            list.Add(_graphicsDevice.PointSampler);
        }
        return list.ToArray();
    }

    public override void Draw(CommandList cl)
    {
        if (_vertexBuffer == null || _pipeline == null) return;

        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetPipeline(_pipeline);

        cl.SetGraphicsResourceSet((uint)(PipelineDescription.ResourceLayouts.Length - 1), _channelSet);

        if (_indexBuffer != null)
        {
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.DrawIndexed(
                indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
                instanceCount: 1, indexStart: 0, vertexOffset: 0, instanceStart: 0);
        }
        else
        {
            uint stride = (uint)VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes());
            cl.Draw((uint)(_vertexBuffer.SizeInBytes / stride));
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _channelSet?.Dispose();
        _channelLayout?.Dispose();
    }
}
